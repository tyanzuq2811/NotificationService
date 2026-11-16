using System;
using System.Data;
using System.Threading.Tasks;
using Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandService.Services
{
    public class DatabaseInitializer
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var masterConnStr = _config.GetConnectionString("MasterSqlServer");
            var appConnStr = _config.GetConnectionString("SqlServer");

            try
            {
                _logger.LogInformation("Kiểm tra cơ sở dữ liệu NotificationWriteDb...");
                using (var masterConn = new SqlConnection(masterConnStr))
                {
                    await masterConn.OpenAsync();
                    var checkDbSql = @"
                        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'NotificationWriteDb')
                        BEGIN
                            CREATE DATABASE NotificationWriteDb;
                        END";
                    await masterConn.ExecuteAsync(checkDbSql);
                }

                _logger.LogInformation("Cơ sở dữ liệu sẵn sàng. Khởi tạo bảng và nạp Template Tiếng Việt...");
                using (var appConn = new SqlConnection(appConnStr))
                {
                    await appConn.OpenAsync();

                    var createTablesSql = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NotificationEvents' and xtype='U')
                        BEGIN
                            CREATE TABLE NotificationEvents (
                                EventId UNIQUEIDENTIFIER PRIMARY KEY,
                                EventType NVARCHAR(100) NOT NULL,
                                AggregateId UNIQUEIDENTIFIER NOT NULL,
                                Payload NVARCHAR(MAX) NOT NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                IsPublished BIT NOT NULL DEFAULT 0,
                                PublishedAt DATETIME2 NULL
                            );
                            CREATE INDEX IX_Outbox_Pending ON NotificationEvents(IsPublished, CreatedAt);
                        END;

                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NotificationTemplates' and xtype='U')
                        BEGIN
                            CREATE TABLE NotificationTemplates (
                                TemplateId INT IDENTITY(1,1) PRIMARY KEY,
                                EventCode NVARCHAR(50) NOT NULL,
                                Channel NVARCHAR(20) NOT NULL,
                                TitleTemplate NVARCHAR(255) NOT NULL,
                                BodyTemplate NVARCHAR(MAX) NOT NULL,
                                IsActive BIT NOT NULL DEFAULT 1
                            );
                        END;

                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserPreferences' and xtype='U')
                        BEGIN
                            CREATE TABLE UserPreferences (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                UserId NVARCHAR(100) NOT NULL,
                                EventCode NVARCHAR(50) NOT NULL,
                                EnableInApp BIT NOT NULL DEFAULT 1,
                                EnableEmail BIT NOT NULL DEFAULT 1,
                                EnableTelegram BIT NOT NULL DEFAULT 1,
                                TelegramChatId BIGINT NULL,
                                Email NVARCHAR(255) NULL,
                                CONSTRAINT UQ_User_Event UNIQUE(UserId, EventCode)
                            );
                        END;";

                    await appConn.ExecuteAsync(createTablesSql);

                    // Di chuyển lược đồ CSDL cho các tính năng nâng cao (thực thi theo từng đợt để SQL Server phân tích cú pháp)
                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NotificationEvents') AND name = 'ScheduledAt')
                            ALTER TABLE NotificationEvents ADD ScheduledAt DATETIME2 NULL;
                    ");

                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NotificationEvents') AND name = 'ExpiresAt')
                            ALTER TABLE NotificationEvents ADD ExpiresAt DATETIME2 NULL;
                    ");

                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NotificationEvents') AND name = 'Priority')
                            ALTER TABLE NotificationEvents ADD Priority NVARCHAR(20) NOT NULL DEFAULT 'STANDARD';
                    ");

                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NotificationEvents') AND name = 'IdempotencyKey')
                            ALTER TABLE NotificationEvents ADD IdempotencyKey NVARCHAR(100) NULL;
                    ");

                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_NotificationEvents_IdempotencyKey')
                            EXEC('CREATE UNIQUE NONCLUSTERED INDEX UQ_NotificationEvents_IdempotencyKey ON NotificationEvents(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL');
                    ");

                    await appConn.ExecuteAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'QuietHoursStart')
                            ALTER TABLE UserPreferences ADD QuietHoursStart TIME NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'QuietHoursEnd')
                            ALTER TABLE UserPreferences ADD QuietHoursEnd TIME NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'EnableZalo')
                            ALTER TABLE UserPreferences ADD EnableZalo BIT NOT NULL DEFAULT 1;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'EnableWebPush')
                            ALTER TABLE UserPreferences ADD EnableWebPush BIT NOT NULL DEFAULT 1;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'EnableWebhook')
                            ALTER TABLE UserPreferences ADD EnableWebhook BIT NOT NULL DEFAULT 1;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'PhoneNumber')
                            ALTER TABLE UserPreferences ADD PhoneNumber NVARCHAR(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPreferences') AND name = 'WebhookUrl')
                            ALTER TABLE UserPreferences ADD WebhookUrl NVARCHAR(MAX) NULL;

                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WebPushSubscriptions' and xtype='U')
                        BEGIN
                            CREATE TABLE WebPushSubscriptions (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                UserId NVARCHAR(100) NOT NULL,
                                Endpoint NVARCHAR(MAX) NOT NULL,
                                P256dh NVARCHAR(255) NOT NULL,
                                Auth NVARCHAR(255) NOT NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END;
                    ");

                    // Luôn đồng bộ và cập nhật lại template chuẩn chuyên nghiệp (18 Templates: 6 Kênh x 3 Nghiệp vụ)
                    var syncTemplatesSql = @"
                        DELETE FROM NotificationTemplates;
                        INSERT INTO NotificationTemplates (EventCode, Channel, TitleTemplate, BodyTemplate, IsActive) VALUES
                        -- 1. ĐẶT HÀNG THÀNH CÔNG (ORDER_SUCCESS)
                        ('ORDER_SUCCESS', 'IN_APP', N'Đặt hàng thành công #{{orderId}}', N'Đơn hàng trị giá {{amount}} đ của {{userName}} đã được xác nhận và đang đóng gói.', 1),
                        ('ORDER_SUCCESS', 'EMAIL', N'Xác nhận đơn hàng #{{orderId}} thành công', N'Xin chào {{userName}}, đơn hàng của bạn đã được hệ thống tiếp nhận vào lúc {{time}} và đang được chuyển sang bộ phận đóng gói.', 1),
                        ('ORDER_SUCCESS', 'TELEGRAM', N'XÁC NHẬN ĐƠN HÀNG THÀNH CÔNG', N'<b>XÁC NHẬN ĐƠN HÀNG THÀNH CÔNG</b>' + CHAR(10) + CHAR(10) + N'Xin chào <b>{{userName}}</b>,' + CHAR(10) + N'Đơn hàng <code>#{{orderId}}</code> đã được tiếp nhận và đang tiến hành đóng gói.' + CHAR(10) + CHAR(10) + N'<blockquote>• Tổng thanh toán: <code>{{amount}} đ</code>' + CHAR(10) + N'• Thời gian: <code>{{time}}</code>' + CHAR(10) + N'• Trạng thái: Đã xác nhận</blockquote>' + CHAR(10) + CHAR(10) + N'Mã giao dịch: <code>b49aac63</code>' + CHAR(10) + N'<i>NotifyHub • Hệ thống Thông báo Tự động</i>', 1),
                        ('ORDER_SUCCESS', 'ZALO_ZNS', N'Xác nhận đặt hàng thành công', N'Cảm ơn {{userName}} đã đặt hàng. Thông tin chi tiết đơn hàng của quý khách:' + CHAR(10) + N'• Mã đơn hàng: #{{orderId}}' + CHAR(10) + N'• Tổng tiền: {{amount}} đ' + CHAR(10) + N'• Thời gian đặt: {{time}}' + CHAR(10) + N'• Trạng thái: Đang đóng gói', 1),
                        ('ORDER_SUCCESS', 'WEB_PUSH', N'Đơn hàng #{{orderId}} đã xác nhận', N'Xin chào {{userName}}, đơn hàng {{amount}} đ đã được tiếp nhận lúc {{time}} và đang đóng gói.', 1),
                        ('ORDER_SUCCESS', 'WEBHOOK', N'Đặt hàng thành công #{{orderId}}', N'Xin chào **{{userName}}**, đơn hàng của bạn đã được hệ thống ghi nhận và chuyển sang bộ phận đóng gói.', 1),

                        -- 2. MÃ XÁC THỰC BẢO MẬT (SECURITY_OTP)
                        ('SECURITY_OTP', 'IN_APP', N'Mã OTP bảo mật: {{otp}}', N'Mã xác thực của {{userName}} là {{otp}}, hiệu lực trong 5 phút. Tuyệt đối không chia sẻ mã này.', 1),
                        ('SECURITY_OTP', 'EMAIL', N'Mã xác thực bảo mật (OTP) cho tài khoản', N'Xin chào {{userName}}, mã xác thực bảo mật của bạn là: {{otp}} (có hiệu lực trong 5 phút). Vui lòng không chia sẻ mã này cho bất kỳ ai kể cả nhân viên hỗ trợ.', 1),
                        ('SECURITY_OTP', 'TELEGRAM', N'MÃ XÁC THỰC BẢO MẬT (OTP)', N'<b>MÃ XÁC THỰC BẢO MẬT (OTP)</b>' + CHAR(10) + CHAR(10) + N'Xin chào <b>{{userName}}</b>,' + CHAR(10) + N'Mã xác thực giao dịch của bạn là:' + CHAR(10) + CHAR(10) + N'<blockquote>Mã OTP: <code>{{otp}}</code>' + CHAR(10) + N'Hiệu lực trong: <b>05 phút</b> (đến {{time}})</blockquote>' + CHAR(10) + CHAR(10) + N'<i>Không chia sẻ mã này cho bất kỳ ai kể cả nhân viên hỗ trợ.</i>', 1),
                        ('SECURITY_OTP', 'ZALO_ZNS', N'Mã OTP xác thực giao dịch', N'Kính gửi {{userName}}, mã OTP xác thực của bạn là: {{otp}} (hiệu lực 5 phút đến {{time}}). Quý khách tuyệt đối không cung cấp mã này cho người khác.', 1),
                        ('SECURITY_OTP', 'WEB_PUSH', N'Mã OTP bảo mật: {{otp}}', N'Mã OTP của {{userName}} là {{otp}} (hiệu lực 5 phút). Tuyệt đối không chia sẻ cho người khác!', 1),
                        ('SECURITY_OTP', 'WEBHOOK', N'Cảnh báo bảo mật: Yêu cầu OTP', N'Yêu cầu xác thực OTP tài khoản của **{{userName}}**. Mã xác thực: `{{otp}}` (hiệu lực 5 phút).', 1),

                        -- 3. BIẾN ĐỘNG SỐ DƯ (BALANCE_CHANGE)
                        ('BALANCE_CHANGE', 'IN_APP', N'Biến động số dư: {{type}} {{amount}} đ', N'Tài khoản của {{userName}} vừa có giao dịch {{type}} {{amount}} đ lúc {{time}}. Số dư khả dụng hiện tại: {{balance}} đ.', 1),
                        ('BALANCE_CHANGE', 'EMAIL', N'Thông báo biến động số dư tài khoản', N'Xin chào {{userName}}, tài khoản của bạn vừa phát sinh giao dịch {{type}} {{amount}} đ vào lúc {{time}}. Nội dung: {{reason}}. Số dư tài khoản khả dụng: {{balance}} đ.', 1),
                        ('BALANCE_CHANGE', 'TELEGRAM', N'THÔNG BÁO BIẾN ĐỘNG SỐ DƯ', N'<b>THÔNG BÁO BIẾN ĐỘNG SỐ DƯ</b>' + CHAR(10) + CHAR(10) + N'Tài khoản: <code>**** 8899</code>' + CHAR(10) + N'<blockquote>• Giao dịch: <code>{{type}} {{amount}} đ</code>' + CHAR(10) + N'• Số dư hiện tại: <code>{{balance}}</code>' + CHAR(10) + N'• Thời gian: <code>{{time}}</code>' + CHAR(10) + N'• Nội dung: {{reason}}</blockquote>' + CHAR(10) + CHAR(10) + N'Mã tham chiếu: <code>b49aac63</code>' + CHAR(10) + N'<i>NotifyHub • Hệ thống Thông báo Tự động</i>', 1),
                        ('BALANCE_CHANGE', 'ZALO_ZNS', N'Thông báo biến động số dư', N'Kính gửi {{userName}}, tài khoản phát sinh giao dịch: {{type}} {{amount}} đ lúc {{time}}. Nội dung: {{reason}}. Số dư khả dụng: {{balance}} đ.', 1),
                        ('BALANCE_CHANGE', 'WEB_PUSH', N'Biến động số dư: {{type}} {{amount}} đ', N'Tài khoản {{userName}} vừa {{type}} {{amount}} đ lúc {{time}}. Số dư: {{balance}} đ.', 1),
                        ('BALANCE_CHANGE', 'WEBHOOK', N'Thông Báo Biến Động Số Dư', N'Chủ tài khoản: **{{userName}}**' + CHAR(10) + N'Giao dịch: {{type}} {{amount}} đ' + CHAR(10) + N'Lý do: {{reason}}' + CHAR(10) + N'Số dư mới: {{balance}} đ', 1);";

                    await appConn.ExecuteAsync(syncTemplatesSql);

                    // Cập nhật hoặc khởi tạo cấu hình kênh nhận thông báo người dùng (UserPreferences)
                    var defaultWebhookUrl = "https://discord.com/api/webhooks/1571076686710185994/2E2S42LpGpTuype1UbZvMucnDriubHkbPIirBj1jLdftalf2gyNwVN5tc4zalz1d1oBD";
                    var updatePrefsSql = @"
                        UPDATE UserPreferences 
                        SET WebhookUrl = @WebhookUrl, 
                            PhoneNumber = '0766408268',
                            EnableZalo = 1,
                            EnableWebPush = 1,
                            EnableWebhook = 1
                        WHERE UserId = 'user_demo';

                        IF NOT EXISTS (SELECT 1 FROM UserPreferences WHERE UserId = 'user_demo')
                        BEGIN
                            INSERT INTO UserPreferences (UserId, EventCode, EnableInApp, EnableEmail, EnableTelegram, EnableZalo, EnableWebPush, EnableWebhook, TelegramChatId, Email, PhoneNumber, WebhookUrl) VALUES
                            ('user_demo', 'ORDER_SUCCESS', 1, 1, 1, 1, 1, 1, 1547761725, 'ltd28112005@gmail.com', '0766408268', @WebhookUrl),
                            ('user_demo', 'SECURITY_OTP', 1, 1, 1, 1, 1, 1, 1547761725, 'ltd28112005@gmail.com', '0766408268', @WebhookUrl),
                            ('user_demo', 'BALANCE_CHANGE', 1, 1, 1, 1, 1, 1, 1547761725, 'ltd28112005@gmail.com', '0766408268', @WebhookUrl),
                            ('user_demo', 'SYSTEM_ALERT', 1, 1, 1, 1, 1, 1, 1547761725, 'ltd28112005@gmail.com', '0766408268', @WebhookUrl);
                        END;

                        IF NOT EXISTS (SELECT 1 FROM UserPreferences WHERE UserId = 'user_quynh')
                        BEGIN
                            INSERT INTO UserPreferences (UserId, EventCode, EnableInApp, EnableEmail, EnableTelegram, EnableZalo, EnableWebPush, EnableWebhook, TelegramChatId, Email, PhoneNumber, WebhookUrl) VALUES
                            ('user_quynh', 'ORDER_SUCCESS', 1, 1, 1, 1, 1, 1, 1547761725, 'quynh.dt@example.com', '0766408268', @WebhookUrl),
                            ('user_quynh', 'SECURITY_OTP', 1, 1, 1, 1, 1, 1, 1547761725, 'quynh.dt@example.com', '0766408268', @WebhookUrl),
                            ('user_quynh', 'BALANCE_CHANGE', 1, 1, 1, 1, 1, 1, 1547761725, 'quynh.dt@example.com', '0766408268', @WebhookUrl),
                            ('user_quynh', 'SYSTEM_ALERT', 1, 1, 1, 1, 1, 1, 1547761725, 'quynh.dt@example.com', '0766408268', @WebhookUrl);
                        END;";

                    await appConn.ExecuteAsync(updatePrefsSql, new { WebhookUrl = defaultWebhookUrl });
                }
                _logger.LogInformation("Đã khởi tạo và đồng bộ Templates Tiếng Việt thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo Database.");
                throw;
            }
        }
    }
}
