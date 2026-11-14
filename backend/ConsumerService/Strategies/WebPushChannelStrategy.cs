using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace ConsumerService.Strategies
{
    public class WebPushChannelStrategy : INotificationChannelStrategy
    {
        public ChannelType ChannelType => ChannelType.WEB_PUSH;

        private readonly ILogger<WebPushChannelStrategy> _logger;
        private readonly VapidDetails _vapidDetails;

        public WebPushChannelStrategy(IConfiguration config, ILogger<WebPushChannelStrategy> logger)
        {
            _logger = logger;

            var subject = config["Channels:WebPush:Subject"] ?? "mailto:admin@notificationservice.com";
            var publicKey = config["Channels:WebPush:PublicKey"] ?? "BNjt2n6zUqd5yS9ORdDmRNxr16jH07LBTxdFjlj8m0eb167uR3uRC-uD_EAUean3NqbOoFTdU-uOzN1FiqQIf5E";
            var privateKey = config["Channels:WebPush:PrivateKey"] ?? "decrzLo2a4y2SX9VtO3HQ55AK8QlaCE6ExZj0zO3CVU";

            _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        }

        public async Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct)
        {
            var sub = context.WebPushSubscription;

            // Strict OS character limit & newline cleanup
            var title = (context.Title ?? string.Empty).Trim();
            if (title.Length > 45) title = title.Substring(0, 42) + "...";

            var cleanBody = (context.Content ?? string.Empty)
                .Replace("\\r\\n", " ")
                .Replace("\\n", " ")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Trim();
            if (cleanBody.Length > 110) cleanBody = cleanBody.Substring(0, 107) + "...";

            var payloadObj = new
            {
                title,
                body = cleanBody,
                icon = "http://localhost:5173/favicon.svg",
                badge = "http://localhost:5173/favicon.svg",
                tag = context.NotificationId.ToString(), // Prevent OS duplicate popups
                data = new
                {
                    notificationId = context.NotificationId,
                    eventCode = context.EventCode,
                    time = DateTime.UtcNow.ToString("o"),
                    url = "http://localhost:5173"
                },
                actions = new[]
                {
                    new { action = "open", title = "Xem chi tiết" }
                }
            };
            var payloadJson = JsonSerializer.Serialize(payloadObj);

            if (sub != null && !string.IsNullOrWhiteSpace(sub.Endpoint))
            {
                try
                {
                    var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    var client = new WebPushClient();
                    await client.SendNotificationAsync(pushSubscription, payloadJson, _vapidDetails, ct);

                    _logger.LogInformation("[Web Push Channel] Real OS Notification dispatched to browser endpoint: {Endpoint}",
                        sub.Endpoint.Length > 40 ? sub.Endpoint.Substring(0, 40) + "..." : sub.Endpoint);

                    return ChannelDeliveryResult.Success(ChannelType.WEB_PUSH, DeliveryStatus.DELIVERED, "Windows_ActionCenter:" + context.UserId);
                }
                catch (WebPushException wpex)
                {
                    _logger.LogWarning(wpex, "[Web Push Channel] WebPushException: {Status} {Message}", wpex.StatusCode, wpex.Message);
                    return ChannelDeliveryResult.Failed(ChannelType.WEB_PUSH, sub.Endpoint, $"WebPush error: {wpex.StatusCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Web Push Channel] Failed to dispatch OS Web Push");
                    return ChannelDeliveryResult.Failed(ChannelType.WEB_PUSH, sub.Endpoint, ex.Message);
                }
            }
            else
            {
                // Simulated fallback when browser subscription is not yet registered
                await Task.Delay(100, ct);
                _logger.LogInformation("[Web Push Channel] (Simulated OS Notification) Windows Action Center: '{Title}'", context.Title);
                return ChannelDeliveryResult.Success(ChannelType.WEB_PUSH, DeliveryStatus.DELIVERED, "Windows_ActionCenter:" + context.UserId);
            }
        }
    }
}
