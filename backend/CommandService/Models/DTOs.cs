using System;
using System.Collections.Generic;
using Common;

namespace CommandService.Models
{
    public class SendNotificationRequest
    {
        public string UserId { get; set; } = "user_demo";
        public string EventCode { get; set; } = EventCodes.ORDER_SUCCESS;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string? Email { get; set; }
        public long? TelegramChatId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WebhookUrl { get; set; }
        public List<ChannelType>? Channels { get; set; }
        public string? IdempotencyKey { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Priority { get; set; } = "STANDARD";
        public bool SimulateSmtpError { get; set; } = false;
        public bool SimulateRateLimit { get; set; } = false;
        public bool SimulateExpiredTtl { get; set; } = false;
    }

    public class WebPushSubscriptionRequest
    {
        public string UserId { get; set; } = "user_demo";
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class SendBulkNotificationRequest
    {
        public List<string> UserIds { get; set; } = new();
        public string EventCode { get; set; } = EventCodes.ORDER_SUCCESS;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string? Email { get; set; }
        public long? TelegramChatId { get; set; }
        public List<ChannelType>? Channels { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }

    public class UpdateTemplateRequest
    {
        public string TitleTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class MarkAsReadRequest
    {
        public string UserId { get; set; } = "user_demo";
    }

    public class CommandResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
