using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ConsumerService.Strategies
{
    public class TelegramChannelStrategy : INotificationChannelStrategy
    {
        public ChannelType ChannelType => ChannelType.TELEGRAM;

        private readonly IConfiguration _config;
        private readonly ILogger<TelegramChannelStrategy> _logger;

        public TelegramChannelStrategy(IConfiguration config, ILogger<TelegramChannelStrategy> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct)
        {
            var chatId = context.TelegramChatId;
            var botToken = _config["Channels:Telegram:BotToken"];

            if (!string.IsNullOrWhiteSpace(botToken) && chatId.HasValue && chatId.Value > 0)
            {
                try
                {
                    var botClient = new TelegramBotClient(botToken);
                    var formattedText = GenerateBeautifulTelegramMessage(context);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId.Value,
                        text: formattedText,
                        parseMode: ParseMode.Html,
                        cancellationToken: ct
                    );

                    _logger.LogInformation("[Telegram Channel] Tin nhắn thực tế đã gửi thành công tới ChatId: {ChatId}", chatId.Value);
                    return ChannelDeliveryResult.Success(ChannelType.TELEGRAM, DeliveryStatus.SENT, chatId.Value.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Telegram Channel] Thất bại khi gửi tin nhắn Telegram tới ChatId: {ChatId}", chatId);
                    return ChannelDeliveryResult.Failed(ChannelType.TELEGRAM, chatId?.ToString(), ex.Message);
                }
            }
            else
            {
                await Task.Delay(100, ct);
                var target = (chatId.HasValue && chatId.Value > 0) ? chatId.Value.ToString() : "Simulated_ChatId_618920145";
                _logger.LogInformation("[Telegram Channel] (Mô phỏng) Đã phát tán tới ChatId {Target}: {Title}",
                    target, context.Title);

                return ChannelDeliveryResult.Success(ChannelType.TELEGRAM, DeliveryStatus.SENT, target);
            }
        }

        private static string GenerateBeautifulTelegramMessage(NotificationContext context)
        {
            var shortId = context.NotificationId.ToString().Substring(0, 8);
            var nowStr = DateTime.Now.ToString("HH:mm:ss • dd/MM/yyyy");
            var userName = context.Parameters.TryGetValue("userName", out var un) ? un : (context.UserId == "user_demo" ? "Lê Tuấn Dũng" : context.UserId);

            // 1. ORDER_SUCCESS
            if (context.EventCode == EventCodes.ORDER_SUCCESS)
            {
                var orderId = context.Parameters.TryGetValue("orderId", out var oid) ? oid : "DH-VN-9999";
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "3.250.000";
                if (!orderId.StartsWith("#")) orderId = "#" + orderId;

                return $@"<b>XÁC NHẬN ĐƠN HÀNG THÀNH CÔNG</b>

Xin chào <b>{WebUtility.HtmlEncode(userName)}</b>,
Đơn hàng <code>{WebUtility.HtmlEncode(orderId)}</code> đã được tiếp nhận và đang tiến hành đóng gói.

<blockquote>• Tổng thanh toán: <code>{WebUtility.HtmlEncode(amount)} đ</code>
• Thời gian: <code>{nowStr}</code>
• Trạng thái: Đã xác nhận</blockquote>

Mã giao dịch: <code>{shortId}</code>
<i>NotifyHub • Hệ thống Thông báo Tự động</i>";
            }

            // 2. SECURITY_OTP
            if (context.EventCode == EventCodes.SECURITY_OTP)
            {
                var otp = context.Parameters.TryGetValue("otp", out var o) ? o : "849201";
                var expireTime = DateTime.Now.AddMinutes(5).ToString("HH:mm");

                return $@"<b>MÃ XÁC THỰC BẢO MẬT (OTP)</b>

Xin chào <b>{WebUtility.HtmlEncode(userName)}</b>,
Mã xác thực giao dịch của bạn là:

<blockquote>Mã OTP: <code>{WebUtility.HtmlEncode(otp)}</code>
Hiệu lực trong: <b>05 phút</b> (đến {expireTime})</blockquote>

<i>Không chia sẻ mã này cho bất kỳ ai kể cả nhân viên hỗ trợ.</i>";
            }

            // 3. BALANCE_CHANGE
            if (context.EventCode == EventCodes.BALANCE_CHANGE)
            {
                var type = context.Parameters.TryGetValue("type", out var t) ? t : "+";
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "450.000";
                var balance = context.Parameters.TryGetValue("balance", out var b) ? b : "12.850.000 đ";
                var reason = context.Parameters.TryGetValue("reason", out var r) ? r : "Thanh toán giao dịch trực tuyến";

                return $@"<b>THÔNG BÁO BIẾN ĐỘNG SỐ DƯ</b>

Tài khoản: <code>**** 8899</code>
<blockquote>• Giao dịch: <code>{type} {WebUtility.HtmlEncode(amount)} đ</code>
• Số dư hiện tại: <code>{WebUtility.HtmlEncode(balance)}</code>
• Thời gian: <code>{nowStr}</code>
• Nội dung: {WebUtility.HtmlEncode(reason)}</blockquote>

Mã tham chiếu: <code>{shortId}</code>
<i>NotifyHub • Hệ thống Thông báo Tự động</i>";
            }

            // Default fallback
            var cleanDesc = (context.Content ?? string.Empty)
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\r\n", "\n");

            return $@"<b>{WebUtility.HtmlEncode(context.Title)}</b>

{WebUtility.HtmlEncode(cleanDesc)}

<blockquote>• Thời gian: <code>{nowStr}</code>
• Mã tham chiếu: <code>{shortId}</code></blockquote>
<i>NotifyHub • Hệ thống Thông báo Tự động</i>";
        }
    }
}
