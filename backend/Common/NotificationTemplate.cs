namespace Common
{
    public class NotificationTemplate
    {
        public int TemplateId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string TitleTemplate { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UserPreference
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventCode { get; set; } = string.Empty;
        public bool EnableInApp { get; set; } = true;
        public bool EnableEmail { get; set; } = true;
        public bool EnableTelegram { get; set; } = true;
        public bool EnableZalo { get; set; } = true;
        public bool EnableWebPush { get; set; } = true;
        public bool EnableWebhook { get; set; } = true;
        public long? TelegramChatId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; } = "0912889999";
        public string? WebhookUrl { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }
    }
}
