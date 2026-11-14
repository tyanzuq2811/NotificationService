using System;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace ConsumerService.Strategies
{
    public class NotificationContext
    {
        public Guid NotificationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Email { get; set; }
        public long? TelegramChatId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WebhookUrl { get; set; }
        public WebPushSubscriptionItem? WebPushSubscription { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Parameters { get; set; } = new();
        public bool SimulateSmtpError { get; set; } = false;
        public bool SimulateRateLimit { get; set; } = false;
    }

    public class ChannelDeliveryResult
    {
        public ChannelType Channel { get; set; }
        public DeliveryStatus Status { get; set; }
        public string? Recipient { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ChannelDeliveryResult Success(ChannelType channel, DeliveryStatus status, string? recipient) =>
            new() { Channel = channel, Status = status, Recipient = recipient, Timestamp = DateTime.UtcNow };

        public static ChannelDeliveryResult Failed(ChannelType channel, string? recipient, string errorMessage) =>
            new() { Channel = channel, Status = DeliveryStatus.FAILED, Recipient = recipient, ErrorMessage = errorMessage, Timestamp = DateTime.UtcNow };
    }

    public interface INotificationChannelStrategy
    {
        ChannelType ChannelType { get; }
        Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct);
    }
}
