using System;
using System.Collections.Generic;

namespace Common
{
    public class NotificationPayload
    {
        public Guid NotificationId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string EventCode { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string? Email { get; set; }
        public long? TelegramChatId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WebhookUrl { get; set; }
        public WebPushSubscriptionItem? WebPushSubscription { get; set; }
        public List<ChannelType> EnabledChannels { get; set; } = new();
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Priority { get; set; } = "STANDARD";
        public string? IdempotencyKey { get; set; }
        public bool SimulateSmtpError { get; set; } = false;
        public bool SimulateRateLimit { get; set; } = false;
        public bool SimulateExpiredTtl { get; set; } = false;
        public List<TraceLogItem> TraceLogs { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationReadPayload
    {
        public Guid NotificationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
