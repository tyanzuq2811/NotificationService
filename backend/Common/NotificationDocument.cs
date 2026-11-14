using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Common
{
    public class ChannelStatusItem
    {
        [BsonElement("channel")]
        public string Channel { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = DeliveryStatus.PENDING.ToString();

        [BsonElement("recipient")]
        [BsonIgnoreIfNull]
        public string? Recipient { get; set; }

        [BsonElement("error_message")]
        [BsonIgnoreIfNull]
        public string? ErrorMessage { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class NotificationDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("notification_id")]
        public Guid NotificationId { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("event_code")]
        public string EventCode { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("is_read")]
        public bool IsRead { get; set; } = false;

        [BsonElement("read_at")]
        [BsonIgnoreIfNull]
        public DateTime? ReadAt { get; set; }

        [BsonElement("expires_at")]
        [BsonIgnoreIfNull]
        public DateTime? ExpiresAt { get; set; }

        [BsonElement("priority")]
        public string Priority { get; set; } = "STANDARD";

        [BsonElement("is_fallback")]
        public bool IsFallback { get; set; } = false;

        [BsonElement("channels_status")]
        public List<ChannelStatusItem> ChannelsStatus { get; set; } = new();

        [BsonElement("trace_logs")]
        public List<TraceLogItem> TraceLogs { get; set; } = new();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WebPushSubscriptionItem
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class TraceLogItem
    {
        [BsonElement("step")]
        public string Step { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("duration_ms")]
        public long DurationMs { get; set; }
    }

    public class InAppPushMessage
    {
        public Guid NotificationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
