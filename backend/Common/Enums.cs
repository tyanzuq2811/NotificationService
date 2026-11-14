using System.Text.Json.Serialization;

namespace Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChannelType
    {
        IN_APP,
        EMAIL,
        TELEGRAM,
        ZALO_ZNS,
        WEB_PUSH,
        WEBHOOK
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeliveryStatus
    {
        PENDING,
        SENT,
        DELIVERED,
        FAILED,
        FAILED_DLQ,
        EXPIRED,
        FALLBACK_SUCCESS
    }

    public static class EventCodes
    {
        public const string ORDER_SUCCESS = "ORDER_SUCCESS";
        public const string SECURITY_OTP = "SECURITY_OTP";
        public const string BALANCE_CHANGE = "BALANCE_CHANGE";
        public const string SYSTEM_ALERT = "SYSTEM_ALERT";
    }

    public static class EventTypes
    {
        public const string NotificationRequested = "NotificationRequested";
        public const string NotificationRead = "NotificationRead";
        public const string TemplateUpdated = "TemplateUpdated";
    }

    public static class KafkaTopics
    {
        public const string NotificationEvents = "notification-events";
        public const string NotificationEventsCritical = "notification-events-critical";
        public const string NotificationEventsStandard = "notification-events-standard";
        public const string InAppPushEvents = "inapp-push-events";
        public const string NotificationDlq = "notification-dlq";
        public const string SystemConfigEvents = "system-config-events";
    }
}
