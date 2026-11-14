using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ConsumerService.Strategies
{
    public class EmailChannelStrategy : INotificationChannelStrategy
    {
        public ChannelType ChannelType => ChannelType.EMAIL;

        private readonly IConfiguration _config;
        private readonly ILogger<EmailChannelStrategy> _logger;

        public EmailChannelStrategy(IConfiguration config, ILogger<EmailChannelStrategy> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct)
        {
            var recipientEmail = context.Email;
            if (context.SimulateSmtpError)
            {
                _logger.LogWarning("[Chaos Engineering] Kích hoạt giả lập sự cố: Timeout kết nối máy chủ SMTP Google!");
                throw new System.IO.IOException("Simulated SMTP Connection Timeout (Chaos Engineering Fault Injection)");
            }

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogWarning("[Email Channel] Bỏ qua: Địa chỉ email người nhận trống đối với User {UserId}", context.UserId);
                return ChannelDeliveryResult.Failed(ChannelType.EMAIL, null, "Địa chỉ email người nhận không tồn tại");
            }

            var smtpHost = _config["Channels:Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Channels:Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Channels:Email:SenderEmail"] ?? "notification.service@example.com";
            var senderName = _config["Channels:Email:SenderName"] ?? "NotifyHub Platform";
            var password = _config["Channels:Email:Password"] ?? string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(senderName, senderEmail));
                    message.To.Add(new MailboxAddress(context.UserId, recipientEmail));
                    message.Subject = context.Title;

                    var htmlBody = GenerateBeautifulHtmlEmail(context);

                    var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                    message.Body = bodyBuilder.ToMessageBody();

                    using var client = new SmtpClient();
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
                    await client.AuthenticateAsync(senderEmail, password, ct);
                    await client.SendAsync(message, ct);
                    await client.DisconnectAsync(true, ct);

                    _logger.LogInformation("[Email Channel] Email thực tế đã được gửi thành công tới: {Email}", recipientEmail);
                    return ChannelDeliveryResult.Success(ChannelType.EMAIL, DeliveryStatus.SENT, recipientEmail);
                }
                else
                {
                    await Task.Delay(100, ct);
                    _logger.LogInformation("[Email Channel] (Mô phỏng) Email được gửi tới: {Email} - Tiêu đề: '{Subject}'",
                        recipientEmail, context.Title);
                    return ChannelDeliveryResult.Success(ChannelType.EMAIL, DeliveryStatus.SENT, recipientEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Email Channel] Thất bại khi gửi email tới {Email}", recipientEmail);
                return ChannelDeliveryResult.Failed(ChannelType.EMAIL, recipientEmail, ex.Message);
            }
        }

        private static string GenerateBeautifulHtmlEmail(NotificationContext context)
        {
            var shortId = context.NotificationId.ToString().Substring(0, 8);
            var nowStr = DateTime.Now.ToString("HH:mm:ss • dd/MM/yyyy");
            var userName = context.Parameters.TryGetValue("userName", out var un) ? un : (context.UserId == "user_demo" ? "Lê Tuấn Dũng" : context.UserId);

            // 1. Badge & Highlight Card & CTA
            string badgeHtml;
            string highlightCardHtml;
            string ctaText = "Theo dõi trạng thái đơn hàng";
            string ctaUrl = "http://localhost:5173";

            if (context.EventCode == EventCodes.SECURITY_OTP)
            {
                var otp = context.Parameters.TryGetValue("otp", out var o) ? o : "849201";
                badgeHtml = "<span style='background:#fef2f2;color:#b91c1c;padding:4px 12px;border-radius:20px;font-size:11px;font-weight:700;letter-spacing:0.5px;'>MÃ XÁC THỰC BẢO MẬT</span>";
                highlightCardHtml = $@"
                    <div style='background:#fef2f2;border-radius:12px;padding:24px;text-align:center;margin:20px 0;border:1px solid #fee2e2;'>
                        <div style='font-size:12px;color:#991b1b;font-weight:700;text-transform:uppercase;letter-spacing:1px;'>Mã xác thực OTP</div>
                        <div style='font-size:32px;font-weight:900;letter-spacing:6px;color:#b91c1c;margin:8px 0;font-family:monospace;'>{otp}</div>
                        <div style='font-size:11px;color:#dc2626;'>Mã có hiệu lực trong <b>05 phút</b>. Tuyệt đối không chia sẻ mã này.</div>
                    </div>";
                ctaText = "Xác nhận bảo mật tài khoản";
            }
            else if (context.EventCode == EventCodes.BALANCE_CHANGE)
            {
                var type = context.Parameters.TryGetValue("type", out var t) ? t : "+";
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "450.000";
                var balance = context.Parameters.TryGetValue("balance", out var b) ? b : "12.850.000 đ";
                badgeHtml = "<span style='background:#fef3c7;color:#92400e;padding:4px 12px;border-radius:20px;font-size:11px;font-weight:700;letter-spacing:0.5px;'>BIẾN ĐỘNG SỐ DƯ</span>";
                highlightCardHtml = $@"
                    <div style='background:#f8fafc;border-radius:12px;padding:20px;text-align:center;margin:20px 0;border:1px solid #e2e8f0;'>
                        <div style='font-size:12px;color:#64748b;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;'>Biến động giao dịch</div>
                        <div style='font-size:26px;font-weight:800;color:#0f172a;margin:6px 0;'>{type} {amount} đ</div>
                        <div style='font-size:12px;color:#475569;'>Số dư khả dụng hiện tại: <b>{balance}</b></div>
                    </div>";
                ctaText = "Xem lịch sử giao dịch";
            }
            else // ORDER_SUCCESS & default
            {
                var orderId = context.Parameters.TryGetValue("orderId", out var oid) ? oid : "DH-VN-9999";
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "3.250.000";
                if (!orderId.StartsWith("#")) orderId = "#" + orderId;
                badgeHtml = "<span style='background:#ecfdf5;color:#059669;padding:4px 12px;border-radius:20px;font-size:11px;font-weight:700;letter-spacing:0.5px;'>ĐƠN HÀNG ĐÃ XÁC NHẬN</span>";
                highlightCardHtml = $@"
                    <div style='background:#f8fafc;border-radius:12px;padding:20px;text-align:center;margin:20px 0;border:1px solid #e2e8f0;'>
                        <div style='font-size:12px;color:#64748b;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;'>Tổng thanh toán</div>
                        <div style='font-size:26px;font-weight:800;color:#0f172a;margin:6px 0;'>{amount} đ</div>
                        <div style='font-size:12px;color:#059669;font-weight:600;'>Mã đơn hàng: {orderId}</div>
                    </div>";
                ctaText = "Theo dõi trạng thái đơn hàng";
            }

            var cleanContent = (context.Content ?? string.Empty)
                .Replace("\\r\\n", "<br/>")
                .Replace("\\n", "<br/>")
                .Replace("\r\n", "<br/>");

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{context.Title}</title>
</head>
<body style='margin:0;padding:0;background-color:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color:#f8fafc;padding:30px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:580px;background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 10px 25px rgba(0,0,0,0.04);border:1px solid #e2e8f0;'>
                    <!-- Header Banner with Vector Bell -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#1e1b4b 0%,#4338ca 100%);padding:28px 36px;text-align:center;'>
                            <div style='display:inline-block;background:rgba(255,255,255,0.16);padding:10px;border-radius:50%;margin-bottom:10px;'>
                                <svg width='22' height='22' viewBox='0 0 24 24' fill='none' stroke='#ffffff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align:middle;display:block;'>
                                    <path d='M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9'></path>
                                    <path d='M13.73 21a2 2 0 0 1-3.46 0'></path>
                                </svg>
                            </div>
                            <h1 style='margin:0;color:#ffffff;font-size:18px;font-weight:800;letter-spacing:0.5px;text-transform:uppercase;'>
                                NOTIFYHUB &bull; NỀN TẢNG THÔNG BÁO ĐA KÊNH
                            </h1>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding:32px 36px;'>
                            <div style='margin-bottom:14px;'>
                                {badgeHtml}
                            </div>

                            <h2 style='margin:0 0 12px 0;color:#0f172a;font-size:17px;font-weight:700;line-height:1.4;'>
                                {context.Title}
                            </h2>

                            <p style='margin:0 0 16px 0;color:#334155;font-size:13px;line-height:1.6;'>
                                {cleanContent}
                            </p>

                            <!-- Highlight Card -->
                            {highlightCardHtml}

                            <!-- 2-Column Parameter Table -->
                            <table width='100%' style='border-collapse:collapse;font-size:12px;background-color:#ffffff;border-radius:8px;border:1px solid #e2e8f0;margin-top:16px;'>
                                <tr>
                                    <td style='padding:9px 14px;color:#64748b;font-weight:600;border-bottom:1px solid #f1f5f9;width:40%;'>Người nhận:</td>
                                    <td style='padding:9px 14px;color:#0f172a;font-weight:600;border-bottom:1px solid #f1f5f9;'>{userName}</td>
                                </tr>
                                <tr>
                                    <td style='padding:9px 14px;color:#64748b;font-weight:600;border-bottom:1px solid #f1f5f9;'>Thời gian:</td>
                                    <td style='padding:9px 14px;color:#0f172a;border-bottom:1px solid #f1f5f9;'>{nowStr}</td>
                                </tr>
                                <tr>
                                    <td style='padding:9px 14px;color:#64748b;font-weight:600;'>Mã tham chiếu:</td>
                                    <td style='padding:9px 14px;color:#4f46e5;font-family:monospace;font-weight:bold;'>{shortId}</td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <div style='text-align:center;margin:28px 0 12px 0;'>
                                <a href='{ctaUrl}' style='display:inline-block;background:#4f46e5;color:#ffffff;text-decoration:none;font-weight:700;font-size:13px;padding:12px 28px;border-radius:8px;box-shadow:0 2px 6px rgba(79,70,229,0.3);'>
                                    {ctaText} &rarr;
                                </a>
                            </div>
                        </td>
                    </tr>

                    <!-- Subtle Footer -->
                    <tr>
                        <td style='background-color:#f8fafc;border-top:1px solid #e2e8f0;padding:20px 36px;text-align:center;'>
                            <p style='margin:0;color:#94a3b8;font-size:11px;line-height:1.5;'>
                                NotifyHub Platform &bull; Khoa CNTT - Đại học Đại Nam<br/>
                                Đây là thông báo tự động. Vui lòng không phản hồi trực tiếp email này.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
