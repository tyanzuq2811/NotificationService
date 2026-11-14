using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using CommandService.Models;
using Common;
using Confluent.Kafka;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandService.Services
{
    public class NotificationCommandService : INotificationCommandService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string _connectionString;
        private readonly string _bootstrapServers;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NotificationCommandService> _logger;
        private static IProducer<string, string>? _producer;
        private static readonly object _producerLock = new();

        public NotificationCommandService(
            IConfiguration config,
            IMemoryCache cache,
            ILogger<NotificationCommandService> logger)
        {
            _connectionString = config.GetConnectionString("SqlServer")!;
            _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
            _cache = cache;
            _logger = logger;
        }

        private IProducer<string, string> GetProducer()
        {
            if (_producer == null)
            {
                lock (_producerLock)
                {
                    if (_producer == null)
                    {
                        var producerConfig = new ProducerConfig
                        {
                            BootstrapServers = _bootstrapServers,
                            Acks = Acks.All,
                            EnableIdempotence = true,
                            MessageTimeoutMs = 5000
                        };
                        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
                    }
                }
            }
            return _producer;
        }

        public async Task<Guid> SendNotificationAsync(SendNotificationRequest request)
        {
            var acceptedStart = DateTime.UtcNow;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // 1. Check Idempotency Key
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existingId = await connection.ExecuteScalarAsync<Guid?>(
                    "SELECT AggregateId FROM NotificationEvents WHERE IdempotencyKey = @IdempotencyKey",
                    new { request.IdempotencyKey });

                if (existingId.HasValue && existingId.Value != Guid.Empty)
                {
                    _logger.LogInformation("Idempotency match found for key '{Key}'. Returning existing NotificationId: {Id}",
                        request.IdempotencyKey, existingId.Value);
                    return existingId.Value;
                }
            }

            var enabledChannels = request.Channels;
            string? email = request.Email;
            long? telegramChatId = request.TelegramChatId;
            string? phoneNumber = request.PhoneNumber;
            string? webhookUrl = request.WebhookUrl;

            // Resolve preferences if channels not explicitly specified
            var prefSql = "SELECT * FROM UserPreferences WHERE UserId = @UserId AND EventCode = @EventCode";
            var pref = await connection.QueryFirstOrDefaultAsync<UserPreference>(prefSql, new
            {
                UserId = request.UserId,
                EventCode = request.EventCode
            });

            if (enabledChannels == null || enabledChannels.Count == 0)
            {
                enabledChannels = new List<ChannelType>();
                if (pref != null)
                {
                    if (pref.EnableInApp) enabledChannels.Add(ChannelType.IN_APP);
                    if (pref.EnableEmail) enabledChannels.Add(ChannelType.EMAIL);
                    if (pref.EnableTelegram) enabledChannels.Add(ChannelType.TELEGRAM);
                    if (pref.EnableZalo) enabledChannels.Add(ChannelType.ZALO_ZNS);
                    if (pref.EnableWebPush) enabledChannels.Add(ChannelType.WEB_PUSH);
                    if (pref.EnableWebhook) enabledChannels.Add(ChannelType.WEBHOOK);

                    if (string.IsNullOrEmpty(email)) email = pref.Email;
                    if (telegramChatId == null || telegramChatId == 0) telegramChatId = pref.TelegramChatId;
                    if (string.IsNullOrEmpty(phoneNumber)) phoneNumber = pref.PhoneNumber;
                    if (string.IsNullOrEmpty(webhookUrl)) webhookUrl = pref.WebhookUrl;
                }
                else
                {
                    // Default to all 6 channels
                    enabledChannels.AddRange(new[]
                    {
                        ChannelType.IN_APP,
                        ChannelType.EMAIL,
                        ChannelType.TELEGRAM,
                        ChannelType.ZALO_ZNS,
                        ChannelType.WEB_PUSH,
                        ChannelType.WEBHOOK
                    });
                }
            }
            else
            {
                if (string.IsNullOrEmpty(email) && pref != null) email = pref.Email;
                if ((telegramChatId == null || telegramChatId == 0) && pref != null) telegramChatId = pref.TelegramChatId;
                if (string.IsNullOrEmpty(phoneNumber) && pref != null) phoneNumber = pref.PhoneNumber;
                if (string.IsNullOrEmpty(webhookUrl) && pref != null) webhookUrl = pref.WebhookUrl;
            }

            // Fallback default Webhook URL to Discord if still empty
            if (string.IsNullOrEmpty(webhookUrl))
            {
                webhookUrl = "https://discord.com/api/webhooks/1571076686710185994/2E2S42LpGpTuype1UbZvMucnDriubHkbPIirBj1jLdftalf2gyNwVN5tc4zalz1d1oBD";
            }

            // Retrieve Web Push subscription for user
            var webPushSub = await GetWebPushSubscriptionAsync(request.UserId);

            // Compute Priority & TTL (ExpiresAt)
            var priority = request.Priority;
            if (string.IsNullOrWhiteSpace(priority))
            {
                priority = (request.EventCode == EventCodes.SECURITY_OTP || request.EventCode == EventCodes.SYSTEM_ALERT)
                    ? "HIGH" : "STANDARD";
            }

            DateTime? expiresAt = request.ExpiresAt;
            if (request.SimulateExpiredTtl)
            {
                // Expired 15 seconds in the past for Chaos Engineering demo!
                expiresAt = DateTime.UtcNow.AddSeconds(-15);
            }
            else if (expiresAt == null && request.EventCode == EventCodes.SECURITY_OTP)
            {
                expiresAt = DateTime.UtcNow.AddMinutes(5);
            }

            var notificationId = Guid.NewGuid();
            var durationMs = (long)(DateTime.UtcNow - acceptedStart).TotalMilliseconds;
            if (durationMs < 1) durationMs = 1;

            var traceLogs = new List<TraceLogItem>
            {
                new TraceLogItem
                {
                    Step = "1. Command API Accepted",
                    Timestamp = acceptedStart,
                    DurationMs = durationMs
                }
            };

            var payload = new NotificationPayload
            {
                NotificationId = notificationId,
                UserId = request.UserId,
                EventCode = request.EventCode,
                Parameters = request.Parameters ?? new Dictionary<string, string>(),
                Email = email,
                TelegramChatId = telegramChatId,
                PhoneNumber = phoneNumber,
                WebhookUrl = webhookUrl,
                WebPushSubscription = webPushSub,
                EnabledChannels = enabledChannels,
                ScheduledAt = request.ScheduledAt,
                ExpiresAt = expiresAt,
                Priority = priority,
                IdempotencyKey = request.IdempotencyKey,
                SimulateSmtpError = request.SimulateSmtpError,
                SimulateRateLimit = request.SimulateRateLimit,
                SimulateExpiredTtl = request.SimulateExpiredTtl,
                TraceLogs = traceLogs,
                CreatedAt = DateTime.UtcNow
            };

            var outboxEvent = new NotificationEvent
            {
                EventId = Guid.NewGuid(),
                EventType = EventTypes.NotificationRequested,
                AggregateId = payload.NotificationId,
                Payload = JsonSerializer.Serialize(payload, _jsonOptions),
                CreatedAt = DateTime.UtcNow,
                ScheduledAt = request.ScheduledAt,
                ExpiresAt = expiresAt,
                Priority = priority,
                IdempotencyKey = request.IdempotencyKey,
                IsPublished = false
            };

            var insertSql = @"
                INSERT INTO NotificationEvents (EventId, EventType, AggregateId, Payload, CreatedAt, ScheduledAt, ExpiresAt, Priority, IdempotencyKey, IsPublished)
                VALUES (@EventId, @EventType, @AggregateId, @Payload, @CreatedAt, @ScheduledAt, @ExpiresAt, @Priority, @IdempotencyKey, @IsPublished)";

            await connection.ExecuteAsync(insertSql, outboxEvent);
            _logger.LogInformation("Saved Outbox event {EventId} for Notification {NotificationId} (Priority: {Priority}, ScheduledAt: {ScheduledAt})",
                outboxEvent.EventId, payload.NotificationId, priority, request.ScheduledAt);

            return payload.NotificationId;
        }

        public async Task<List<Guid>> SendBulkNotificationAsync(SendBulkNotificationRequest request)
        {
            var results = new List<Guid>();
            foreach (var userId in request.UserIds)
            {
                var single = new SendNotificationRequest
                {
                    UserId = userId,
                    EventCode = request.EventCode,
                    Parameters = new Dictionary<string, string>(request.Parameters),
                    Email = request.Email,
                    TelegramChatId = request.TelegramChatId,
                    Channels = request.Channels,
                    ScheduledAt = request.ScheduledAt
                };
                var id = await SendNotificationAsync(single);
                results.Add(id);
            }
            return results;
        }

        public async Task MarkAsReadAsync(Guid notificationId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var payload = new NotificationReadPayload
            {
                NotificationId = notificationId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };

            var outboxEvent = new NotificationEvent
            {
                EventId = Guid.NewGuid(),
                EventType = EventTypes.NotificationRead,
                AggregateId = notificationId,
                Payload = JsonSerializer.Serialize(payload, _jsonOptions),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            var insertSql = @"
                INSERT INTO NotificationEvents (EventId, EventType, AggregateId, Payload, CreatedAt, IsPublished)
                VALUES (@EventId, @EventType, @AggregateId, @Payload, @CreatedAt, @IsPublished)";

            await connection.ExecuteAsync(insertSql, outboxEvent);
            _logger.LogInformation("Saved Outbox event {EventId} for MarkAsRead {NotificationId}",
                outboxEvent.EventId, notificationId);
        }

        public async Task<List<UserPreference>> GetPreferencesAsync(string userId)
        {
            var cacheKey = $"pref_{userId}";
            if (_cache.TryGetValue(cacheKey, out List<UserPreference>? cached) && cached != null)
            {
                return cached;
            }

            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM UserPreferences WHERE UserId = @UserId";
            var result = (await connection.QueryAsync<UserPreference>(sql, new { UserId = userId })).AsList();

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(5));
            return result;
        }

        public async Task UpdatePreferenceAsync(UserPreference preference)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                MERGE UserPreferences AS target
                USING (SELECT @UserId AS UserId, @EventCode AS EventCode) AS source
                ON (target.UserId = source.UserId AND target.EventCode = source.EventCode)
                WHEN MATCHED THEN
                    UPDATE SET 
                        EnableInApp = @EnableInApp, 
                        EnableEmail = @EnableEmail, 
                        EnableTelegram = @EnableTelegram, 
                        EnableZalo = @EnableZalo,
                        EnableWebPush = @EnableWebPush,
                        EnableWebhook = @EnableWebhook,
                        TelegramChatId = @TelegramChatId, 
                        Email = @Email,
                        PhoneNumber = @PhoneNumber,
                        WebhookUrl = @WebhookUrl,
                        QuietHoursStart = @QuietHoursStart,
                        QuietHoursEnd = @QuietHoursEnd
                WHEN NOT MATCHED THEN
                    INSERT (UserId, EventCode, EnableInApp, EnableEmail, EnableTelegram, EnableZalo, EnableWebPush, EnableWebhook, TelegramChatId, Email, PhoneNumber, WebhookUrl, QuietHoursStart, QuietHoursEnd)
                    VALUES (@UserId, @EventCode, @EnableInApp, @EnableEmail, @EnableTelegram, @EnableZalo, @EnableWebPush, @EnableWebhook, @TelegramChatId, @Email, @PhoneNumber, @WebhookUrl, @QuietHoursStart, @QuietHoursEnd);";

            await connection.ExecuteAsync(sql, preference);

            // Invalidate cache immediately
            _cache.Remove($"pref_{preference.UserId}");
            _logger.LogInformation("Updated preference for User {UserId}, Event {EventCode}",
                preference.UserId, preference.EventCode);
        }

        public async Task<List<NotificationEvent>> GetOutboxEventsAsync(int limit = 50)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = $"SELECT TOP {limit} * FROM NotificationEvents ORDER BY CreatedAt DESC";
            return (await connection.QueryAsync<NotificationEvent>(sql)).AsList();
        }

        public async Task<List<NotificationTemplate>> GetAllTemplatesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM NotificationTemplates ORDER BY EventCode, Channel";
            return (await connection.QueryAsync<NotificationTemplate>(sql)).AsList();
        }

        public async Task<bool> UpdateTemplateAsync(int id, UpdateTemplateRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE NotificationTemplates 
                SET TitleTemplate = @TitleTemplate, BodyTemplate = @BodyTemplate, IsActive = @IsActive 
                WHERE TemplateId = @TemplateId";

            var affected = await connection.ExecuteAsync(sql, new
            {
                TemplateId = id,
                request.TitleTemplate,
                request.BodyTemplate,
                request.IsActive
            });

            if (affected > 0)
            {
                _logger.LogInformation("Template {Id} updated in SQL Server. Publishing cache invalidation event to Kafka...", id);
                try
                {
                    var producer = GetProducer();
                    var eventPayload = JsonSerializer.Serialize(new
                    {
                        type = "TEMPLATE_UPDATED",
                        templateId = id,
                        timestamp = DateTime.UtcNow
                    });

                    await producer.ProduceAsync(KafkaTopics.SystemConfigEvents, new Message<string, string>
                    {
                        Key = id.ToString(),
                        Value = eventPayload
                    });
                    _logger.LogInformation("Published TEMPLATE_UPDATED event to Kafka topic '{Topic}'", KafkaTopics.SystemConfigEvents);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not publish cache invalidation event to Kafka. (Local update succeeded)");
                }

                return true;
            }

            return false;
        }

        public async Task<bool> RetryDlqNotificationAsync(Guid notificationId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE NotificationEvents 
                SET IsPublished = 0, ScheduledAt = NULL 
                WHERE AggregateId = @NotificationId";

            var affected = await connection.ExecuteAsync(sql, new { NotificationId = notificationId });
            _logger.LogInformation("Re-queued DLQ Notification {NotificationId} for processing. Affected rows: {Count}",
                notificationId, affected);

            return affected > 0;
        }

        public async Task SaveWebPushSubscriptionAsync(WebPushSubscriptionRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var sql = @"
                DELETE FROM WebPushSubscriptions WHERE UserId = @UserId;
                INSERT INTO WebPushSubscriptions (UserId, Endpoint, P256dh, Auth, CreatedAt)
                VALUES (@UserId, @Endpoint, @P256dh, @Auth, GETUTCDATE());";

            await connection.ExecuteAsync(sql, request);
            _logger.LogInformation("Saved WebPush subscription for User {UserId}", request.UserId);
        }

        public async Task<WebPushSubscriptionItem?> GetWebPushSubscriptionAsync(string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 1 Endpoint, P256dh, Auth FROM WebPushSubscriptions WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            return await connection.QueryFirstOrDefaultAsync<WebPushSubscriptionItem>(sql, new { UserId = userId });
        }
    }
}
