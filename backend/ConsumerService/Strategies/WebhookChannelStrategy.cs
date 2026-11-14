using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsumerService.Strategies
{
    public class WebhookChannelStrategy : INotificationChannelStrategy
    {
        public ChannelType ChannelType => ChannelType.WEBHOOK;

        private readonly IConfiguration _config;
        private readonly ILogger<WebhookChannelStrategy> _logger;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        public WebhookChannelStrategy(IConfiguration config, ILogger<WebhookChannelStrategy> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct)
        {
            var targetWebhookUrl = context.WebhookUrl;
            if (string.IsNullOrWhiteSpace(targetWebhookUrl))
            {
                targetWebhookUrl = _config["Channels:Webhook:DefaultDiscordUrl"]
                    ?? "https://discord.com/api/webhooks/1571076686710185994/2E2S42LpGpTuype1UbZvMucnDriubHkbPIirBj1jLdftalf2gyNwVN5tc4zalz1d1oBD";
            }

            var embedColor = context.EventCode switch
            {
                EventCodes.ORDER_SUCCESS => 1096065,   // Emerald green #10B981
                EventCodes.SECURITY_OTP => 15680580,   // Crimson red #EF4444
                EventCodes.SYSTEM_ALERT => 15680580,   // Red
                EventCodes.BALANCE_CHANGE => 16096779, // Amber #F59E0B
                _ => 4934373                           // Indigo #4B5563
            };

            var authorName = context.EventCode switch
            {
                EventCodes.ORDER_SUCCESS => "THÔNG BÁO GIAO DỊCH • ĐƠN HÀNG",
                EventCodes.SECURITY_OTP => "CẢNH BÁO BẢO MẬT • MÃ OTP",
                EventCodes.BALANCE_CHANGE => "BIẾN ĐỘNG SỐ DƯ TÀI KHOẢN",
                _ => "THÔNG BÁO HỆ THỐNG"
            };

            // Chuẩn hóa triệt để chuỗi xuống dòng thật
            var cleanDesc = (context.Content ?? string.Empty)
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\r\n", "\n");

            var shortId = context.NotificationId.ToString().Substring(0, 8);
            var nowStr = DateTime.Now.ToString("HH:mm:ss • dd/MM/yyyy");
            var userName = context.Parameters.TryGetValue("userName", out var un) ? un : (context.UserId == "user_demo" ? "Lê Tuấn Dũng" : context.UserId);
            var fields = new List<object>
            {
                new { name = "Khách hàng", value = $"{userName} (`{context.UserId}`)", inline = true }
            };

            if (context.EventCode == EventCodes.ORDER_SUCCESS)
            {
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "3.250.000";
                fields.Add(new { name = "Giá trị đơn hàng", value = $"**{amount} đ**", inline = true });
                fields.Add(new { name = "Trạng thái", value = "Đã xác nhận", inline = true });
            }
            else if (context.EventCode == EventCodes.SECURITY_OTP)
            {
                var otp = context.Parameters.TryGetValue("otp", out var o) ? o : "849201";
                fields.Add(new { name = "Mã xác thực OTP", value = $"`{otp}`", inline = true });
                fields.Add(new { name = "Hiệu lực", value = "05 phút", inline = true });
            }
            else if (context.EventCode == EventCodes.BALANCE_CHANGE)
            {
                var type = context.Parameters.TryGetValue("type", out var t) ? t : "+";
                var amount = context.Parameters.TryGetValue("amount", out var am) ? am : "450.000";
                var balance = context.Parameters.TryGetValue("balance", out var b) ? b : "12.850.000 đ";
                fields.Add(new { name = "Giao dịch", value = $"**{type} {amount} đ**", inline = true });
                fields.Add(new { name = "Số dư hiện tại", value = $"**{balance}**", inline = true });
            }

            fields.Add(new { name = "Mã tham chiếu", value = $"`{shortId}`", inline = true });
            fields.Add(new { name = "Thời gian ghi nhận", value = $"`{nowStr}`", inline = true });

            var discordPayload = new
            {
                username = "NotifyHub Gateway",
                avatar_url = "https://cdn-icons-png.flaticon.com/512/3602/3602145.png",
                embeds = new[]
                {
                    new
                    {
                        author = new { name = authorName },
                        title = context.Title,
                        description = cleanDesc,
                        color = embedColor,
                        fields = fields.ToArray(),
                        footer = new
                        {
                            text = "NotifyHub Microservices • Khoa CNTT - Đại học Đại Nam"
                        },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(discordPayload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(targetWebhookUrl, content, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[Webhook Channel] Real Discord notification delivered successfully to Webhook URL");
                    return ChannelDeliveryResult.Success(ChannelType.WEBHOOK, DeliveryStatus.DELIVERED, "Discord_Webhook:" + targetWebhookUrl.Substring(0, 45) + "...");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("[Webhook Channel] Discord Webhook returned status: {Status} Body: {Body}", response.StatusCode, error);
                    return ChannelDeliveryResult.Failed(ChannelType.WEBHOOK, targetWebhookUrl, $"HTTP {(int)response.StatusCode}: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Webhook Channel] Failed to dispatch Discord Webhook notification");
                return ChannelDeliveryResult.Failed(ChannelType.WEBHOOK, targetWebhookUrl, ex.Message);
            }
        }
    }
}
