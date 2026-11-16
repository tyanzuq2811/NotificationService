using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using ConsumerService.Strategies;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;

namespace ConsumerService.Services
{
    public class NotificationDispatcher
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ChannelStrategyFactory _strategyFactory;
        private readonly IMongoCollection<NotificationDocument> _collection;
        private readonly string _sqlConnStr;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<NotificationDispatcher> _logger;
        private readonly IProducer<string, string> _dlqProducer;
        private const string TEMPLATES_CACHE_KEY = "ALL_TEMPLATES";

        // Chống chịu lỗi Enterprise 2.1: Polly Circuit Breaker và Retry theo từng kênh
        private readonly Dictionary<ChannelType, IAsyncPolicy> _resiliencePolicies = new();

        // Chống chịu lỗi Enterprise 2.3: Giới hạn lưu lượng Token Bucket Rate Limiter theo từng kênh
        private readonly Dictionary<ChannelType, TokenBucketRateLimiter> _channelRateLimiters = new();

        public NotificationDispatcher(
            ChannelStrategyFactory strategyFactory,
            IMongoClient mongoClient,
            IMemoryCache memoryCache,
            IConfiguration config,
            ILogger<NotificationDispatcher> logger)
        {
            _strategyFactory = strategyFactory;
            _memoryCache = memoryCache;
            _logger = logger;
            _sqlConnStr = config.GetConnectionString("SqlServer")!;

            var dbName = config["ConnectionStrings:MongoDbName"] ?? "read_db";
            var database = mongoClient.GetDatabase(dbName);
            _collection = database.GetCollection<NotificationDocument>("notifications");

            var bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 5000
            };
            _dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();

            // Khởi tạo các chính sách chống chịu lỗi (Circuit Breaker + Exponential Backoff Retry)
            InitializeResiliencePolicies();

            // Khởi tạo các bộ điều phối tốc độ Token Bucket Rate Limiter
            InitializeRateLimiters();

            // Đảm bảo khởi tạo các chỉ mục (Indexes) trên MongoDB
            try
            {
                var indexUserCreated = Builders<NotificationDocument>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.CreatedAt);
                _collection.Indexes.CreateOne(new CreateIndexModel<NotificationDocument>(indexUserCreated));

                var indexUserRead = Builders<NotificationDocument>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.IsRead);
                _collection.Indexes.CreateOne(new CreateIndexModel<NotificationDocument>(indexUserRead));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure MongoDB indexes automatically.");
            }
        }

        private void InitializeResiliencePolicies()
        {
            var channels = (ChannelType[])Enum.GetValues(typeof(ChannelType));
            foreach (var ch in channels)
            {
                // Circuit Breaker: Tự động ngắt mạch sau 2 lần lỗi liên tiếp, giữ mạch mở trong 30 giây
                var circuitBreaker = Policy
                    .Handle<Exception>()
                    .CircuitBreakerAsync(
                        exceptionsAllowedBeforeBreaking: 2,
                        durationOfBreak: TimeSpan.FromSeconds(30),
                        onBreak: (ex, breakDelay) =>
                        {
                            _logger.LogWarning("[Circuit Breaker: OPEN] Kênh {Channel} gặp lỗi liên tiếp ({Error}). Ngắt mạch trong {Sec}s (Fail-fast kích hoạt)!",
                                ch, ex.Message, breakDelay.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("[Circuit Breaker: CLOSED] Kênh {Channel} đã hồi phục bình thường.", ch);
                        },
                        onHalfOpen: () =>
                        {
                            _logger.LogInformation("[Circuit Breaker: HALF-OPEN] Kênh {Channel} thăm dò luồng gửi mới...", ch);
                        });

                // Exponential Backoff Retry: Tự động thử lại 3 lần với độ trễ tăng theo hàm mũ (1s, 2s, 4s)
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                        onRetry: (exception, timeSpan, retryCount, ctx) =>
                        {
                            _logger.LogWarning("[Polly Retry] Lần thử {Attempt}/3 cho kênh {Channel} thất bại: {Message}. Đợi {Sec}s...",
                                retryCount, ch, exception.Message, timeSpan.TotalSeconds);
                        });

                // Bọc chính sách Retry ra ngoài Circuit Breaker
                _resiliencePolicies[ch] = Policy.WrapAsync(retryPolicy, circuitBreaker);
            }
        }

        private void InitializeRateLimiters()
        {
            // Email: 5 req/s
            _channelRateLimiters[ChannelType.EMAIL] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                QueueLimit = 20,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 5,
                AutoReplenishment = true
            });

            // Telegram: 25 req/s (Telegram limit ~ 30 req/s)
            _channelRateLimiters[ChannelType.TELEGRAM] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 25,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 25,
                AutoReplenishment = true
            });

            // Zalo ZNS: 20 req/s
            _channelRateLimiters[ChannelType.ZALO_ZNS] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 20,
                AutoReplenishment = true
            });

            // Webhook: 30 req/s
            _channelRateLimiters[ChannelType.WEBHOOK] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 30,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 30,
                AutoReplenishment = true
            });

            // Web Push: 50 req/s
            _channelRateLimiters[ChannelType.WEB_PUSH] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 50,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 50,
                AutoReplenishment = true
            });

            // In-App: 100 req/s
            _channelRateLimiters[ChannelType.IN_APP] = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueLimit = 100,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 100,
                AutoReplenishment = true
            });
        }

        public void InvalidateTemplatesCache()
        {
            _memoryCache.Remove(TEMPLATES_CACHE_KEY);
            _logger.LogInformation("Distributed Cache Invalidation: '{Key}' evicted from IMemoryCache", TEMPLATES_CACHE_KEY);
        }

        public async Task SendPoisonPillToDlqAsync(string rawMessage, string reason, CancellationToken ct)
        {
            try
            {
                var dlqItem = JsonSerializer.Serialize(new
                {
                    ErrorType = "POISON_PILL_MALFORMED",
                    Reason = reason,
                    RawMessage = rawMessage,
                    Timestamp = DateTime.UtcNow
                }, _jsonOptions);

                await _dlqProducer.ProduceAsync(KafkaTopics.NotificationDlq, new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = dlqItem
                }, ct);

                _logger.LogWarning("[Poison Pill Handled] Tin nhắn lỗi định dạng đã chuyển tiếp vào topic '{Topic}' để tránh nghẽn hàng đợi Kafka.",
                    KafkaTopics.NotificationDlq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thất bại khi đẩy Poison Pill vào Kafka DLQ.");
            }
        }

        private async Task<List<NotificationTemplate>> GetCachedTemplatesAsync(CancellationToken ct)
        {
            if (_memoryCache.TryGetValue(TEMPLATES_CACHE_KEY, out List<NotificationTemplate>? cached) && cached != null)
            {
                return cached;
            }

            using var conn = new SqlConnection(_sqlConnStr);
            await conn.OpenAsync(ct);
            var templates = (await conn.QueryAsync<NotificationTemplate>(
                "SELECT * FROM NotificationTemplates WHERE IsActive = 1")).AsList();

            _memoryCache.Set(TEMPLATES_CACHE_KEY, templates, TimeSpan.FromMinutes(30));
            _logger.LogInformation("Đã nạp lại {Count} templates vào IMemoryCache phân tán", templates.Count);
            return templates;
        }

        public async Task DispatchRequestedEventAsync(NotificationPayload payload, CancellationToken ct)
        {
            var dispatchStart = DateTime.UtcNow;
            _logger.LogInformation("Processing NotificationRequestedEvent for NotificationId: {Id}, EventCode: {Code}, Priority: {Priority}",
                payload.NotificationId, payload.EventCode, payload.Priority);

            // 1. Lấy mẫu thông báo (Templates) từ bộ nhớ đệm MemoryCache
            var allTemplates = await GetCachedTemplatesAsync(ct);
            var templates = allTemplates.Where(t => t.EventCode == payload.EventCode).ToList();

            // 2. Kiểm tra khung giờ yên lặng của người dùng (Quiet Hours / Do Not Disturb)
            try
            {
                using var conn = new SqlConnection(_sqlConnStr);
                await conn.OpenAsync(ct);
                var pref = await conn.QueryFirstOrDefaultAsync<UserPreference>(
                    "SELECT * FROM UserPreferences WHERE UserId = @UserId AND EventCode = @EventCode",
                    new { payload.UserId, payload.EventCode });

                if (pref?.QuietHoursStart != null && pref?.QuietHoursEnd != null)
                {
                    var nowTime = DateTime.Now.TimeOfDay;
                    bool inQuietHours;
                    if (pref.QuietHoursStart <= pref.QuietHoursEnd)
                    {
                        inQuietHours = nowTime >= pref.QuietHoursStart && nowTime <= pref.QuietHoursEnd;
                    }
                    else
                    {
                        inQuietHours = nowTime >= pref.QuietHoursStart || nowTime <= pref.QuietHoursEnd;
                    }

                    if (inQuietHours && payload.EventCode != EventCodes.SECURITY_OTP)
                    {
                        _logger.LogInformation("[Quiet Hours / DND] User {UserId} đang trong khung giờ yên lặng ({Start} - {End}). Tắt các kênh ngoại vi.",
                            payload.UserId, pref.QuietHoursStart, pref.QuietHoursEnd);

                        payload.EnabledChannels.Remove(ChannelType.EMAIL);
                        payload.EnabledChannels.Remove(ChannelType.TELEGRAM);
                        payload.EnabledChannels.Remove(ChannelType.ZALO_ZNS);
                        if (!payload.EnabledChannels.Contains(ChannelType.IN_APP))
                        {
                            payload.EnabledChannels.Add(ChannelType.IN_APP);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check user quiet hours. Continuing with standard channels.");
            }

            // 3. Kết xuất Tiêu đề và Nội dung chính cho Read Model MongoDB
            var defaultTemplate = templates.FirstOrDefault(t => t.Channel == ChannelType.IN_APP.ToString()) 
                               ?? templates.FirstOrDefault();

            var mainTitle = defaultTemplate != null 
                ? RenderTemplate(defaultTemplate.TitleTemplate, payload.Parameters) 
                : $"[Thông báo] {payload.EventCode}";

            var mainContent = defaultTemplate != null 
                ? RenderTemplate(defaultTemplate.BodyTemplate, payload.Parameters) 
                : $"Bạn có thông báo mới từ sự kiện {payload.EventCode}.";

            // 4. Nhật ký truy vết Bước 3: Bắt đầu phân phối qua các chiến lược kênh
            payload.TraceLogs ??= new List<TraceLogItem>();
            var step3Duration = (long)(DateTime.UtcNow - dispatchStart).TotalMilliseconds;
            if (step3Duration < 1) step3Duration = 1;

            payload.TraceLogs.Add(new TraceLogItem
            {
                Step = "3. Consumer Strategy Dispatch",
                Timestamp = DateTime.UtcNow,
                DurationMs = step3Duration
            });

            // 5. Chống chịu lỗi Enterprise 2.4: Kiểm tra thời gian sống TTL của thông điệp (ExpiresAt)
            bool isExpired = (payload.ExpiresAt.HasValue && DateTime.UtcNow > payload.ExpiresAt.Value) || payload.SimulateExpiredTtl;
            if (isExpired)
            {
                _logger.LogWarning("[Message TTL Expired] Thông báo {Id} đã hết hạn hiệu lực lúc {ExpiresAt}. Hủy bỏ phát tán để tránh gửi OTP trễ!",
                    payload.NotificationId, payload.ExpiresAt);

                var expiredChannels = payload.EnabledChannels.Select(ch => new ChannelStatusItem
                {
                    Channel = ch.ToString(),
                    Status = DeliveryStatus.EXPIRED.ToString(),
                    Recipient = GetRecipientForChannel(ch, payload),
                    ErrorMessage = "Message TTL Expired before processing (Stale Event)",
                    Timestamp = DateTime.UtcNow
                }).ToList();

                payload.TraceLogs.Add(new TraceLogItem
                {
                    Step = "TTL Expired: Message cancelled",
                    Timestamp = DateTime.UtcNow,
                    DurationMs = (long)(DateTime.UtcNow - payload.CreatedAt).TotalMilliseconds
                });

                var expiredDoc = new NotificationDocument
                {
                    NotificationId = payload.NotificationId,
                    UserId = payload.UserId,
                    EventCode = payload.EventCode,
                    Title = mainTitle + " (ĐÃ HẾT HẠN)",
                    Content = mainContent,
                    IsRead = false,
                    ChannelsStatus = expiredChannels,
                    TraceLogs = payload.TraceLogs,
                    CreatedAt = payload.CreatedAt,
                    ExpiresAt = payload.ExpiresAt,
                    Priority = payload.Priority
                };

                await _collection.InsertOneAsync(expiredDoc, cancellationToken: ct);
                return;
            }

            // 6. Khởi tạo bản ghi MongoDB ban đầu với trạng thái PENDING để ngăn ngừa Lost Update
            var existing = await _collection.Find(x => x.NotificationId == payload.NotificationId).FirstOrDefaultAsync(ct);
            if (existing == null)
            {
                var initialChannels = payload.EnabledChannels.Select(ch => new ChannelStatusItem
                {
                    Channel = ch.ToString(),
                    Status = DeliveryStatus.PENDING.ToString(),
                    Recipient = GetRecipientForChannel(ch, payload),
                    Timestamp = DateTime.UtcNow
                }).ToList();

                var newDoc = new NotificationDocument
                {
                    NotificationId = payload.NotificationId,
                    UserId = payload.UserId,
                    EventCode = payload.EventCode,
                    Title = mainTitle,
                    Content = mainContent,
                    IsRead = false,
                    ChannelsStatus = initialChannels,
                    TraceLogs = new List<TraceLogItem>(payload.TraceLogs),
                    CreatedAt = payload.CreatedAt,
                    ExpiresAt = payload.ExpiresAt,
                    Priority = payload.Priority
                };

                await _collection.InsertOneAsync(newDoc, cancellationToken: ct);
                _logger.LogInformation("Initialized MongoDB Document for NotificationId: {Id} with 6-channel PENDING status", payload.NotificationId);
            }

            // 7. Phát tán đồng thời tới các kênh đã bật kèm Rate Limiting, Circuit Breaker và Fallback
            var tasks = payload.EnabledChannels.Select(async channel =>
            {
                var strategy = _strategyFactory.GetStrategy(channel);
                if (strategy == null)
                {
                    _logger.LogWarning("No strategy registered for channel {Channel}", channel);
                    return;
                }

                // 7.0 Kiểm tra Idempotency và Khóa nguyên tử: Chuyển trạng thái PENDING -> PROCESSING
                var channelFilter = Builders<NotificationDocument>.Filter.And(
                    Builders<NotificationDocument>.Filter.Eq(x => x.NotificationId, payload.NotificationId),
                    Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                        cs => cs.Channel == channel.ToString() && (cs.Status == DeliveryStatus.PENDING.ToString() || cs.Status == "RETRYING"))
                );

                var setProcessingUpdate = Builders<NotificationDocument>.Update
                    .Set("channels_status.$[elem].status", "PROCESSING")
                    .Set("channels_status.$[elem].timestamp", DateTime.UtcNow);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("elem.channel", channel.ToString()))
                };

                var lockedDoc = await _collection.FindOneAndUpdateAsync(
                    channelFilter,
                    setProcessingUpdate,
                    new FindOneAndUpdateOptions<NotificationDocument> { ArrayFilters = arrayFilters },
                    ct);

                if (lockedDoc == null)
                {
                    _logger.LogInformation("[Idempotency Guard] Kênh {Channel} của NotificationId {Id} đã ở trạng thái PROCESSING/SENT/DELIVERED. Bỏ qua tránh gửi trùng lặp.",
                        channel, payload.NotificationId);
                    return;
                }

                var channelTemplate = templates.FirstOrDefault(t => t.Channel == channel.ToString()) ?? defaultTemplate;
                var title = channelTemplate != null ? RenderTemplate(channelTemplate.TitleTemplate, payload.Parameters) : mainTitle;
                var content = channelTemplate != null ? RenderTemplate(channelTemplate.BodyTemplate, payload.Parameters) : mainContent;

                var context = new NotificationContext
                {
                    NotificationId = payload.NotificationId,
                    UserId = payload.UserId,
                    EventCode = payload.EventCode,
                    Title = title,
                    Content = content,
                    Email = payload.Email,
                    TelegramChatId = payload.TelegramChatId,
                    PhoneNumber = payload.PhoneNumber,
                    WebhookUrl = payload.WebhookUrl,
                    WebPushSubscription = payload.WebPushSubscription,
                    Parameters = payload.Parameters ?? new Dictionary<string, string>(),
                    SimulateSmtpError = payload.SimulateSmtpError,
                    SimulateRateLimit = payload.SimulateRateLimit
                };

                var recipient = GetRecipientForChannel(channel, payload);
                ChannelDeliveryResult result;

                try
                {
                    // Thực thi kiểm soát lưu lượng qua Token Bucket Rate Limiter
                    if (_channelRateLimiters.TryGetValue(channel, out var limiter))
                    {
                        using var lease = limiter.AttemptAcquire(1);
                        if (!lease.IsAcquired || payload.SimulateRateLimit)
                        {
                            _logger.LogWarning("[Rate Limit Exceeded] Kênh {Channel} bị bóp băng thông bởi TokenBucketRateLimiter!", channel);
                            throw new InvalidOperationException($"429 Too Many Requests: Channel {channel} rate limit exceeded.");
                        }
                    }

                    // Thực thi phát tán thông qua chuỗi chính sách Polly (Retry + Circuit Breaker)
                    var policy = _resiliencePolicies.TryGetValue(channel, out var p) ? p : Policy.NoOpAsync();
                    result = await policy.ExecuteAsync(async () =>
                    {
                        var res = await strategy.ExecuteAsync(context, ct);
                        if (res.Status == DeliveryStatus.FAILED)
                        {
                            throw new Exception(res.ErrorMessage ?? "Channel delivery returned failure");
                        }
                        return res;
                    });
                }
                catch (BrokenCircuitException bce)
                {
                    _logger.LogError("[Circuit Breaker Tripped - Fail-fast] Kênh {Channel} đang ở trạng thái OPEN ({Error})! Đẩy tức thì sang DLQ/Fallback trong 1ms.",
                        channel, bce.Message);

                    result = new ChannelDeliveryResult
                    {
                        Channel = channel,
                        Status = DeliveryStatus.FAILED_DLQ,
                        Recipient = recipient,
                        ErrorMessage = $"[Circuit Breaker OPEN - Fail Fast]: {bce.Message}",
                        Timestamp = DateTime.UtcNow
                    };

                    await PushToDlqAsync(payload, channel, recipient, result.ErrorMessage, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError("[Polly Exhausted] Kênh {Channel} thất bại sau toàn bộ lần thử lại: {Error}",
                        channel, ex.Message);

                    result = new ChannelDeliveryResult
                    {
                        Channel = channel,
                        Status = DeliveryStatus.FAILED_DLQ,
                        Recipient = recipient,
                        ErrorMessage = $"[Polly Retries Exhausted]: {ex.Message}",
                        Timestamp = DateTime.UtcNow
                    };

                    await PushToDlqAsync(payload, channel, recipient, result.ErrorMessage, ct);
                }

                // Chống chịu lỗi Enterprise 2.2: Chuỗi kênh dự phòng thông minh (Fallback) cho mức ưu tiên cao
                if ((result.Status == DeliveryStatus.FAILED || result.Status == DeliveryStatus.FAILED_DLQ)
                    && (payload.Priority == "HIGH" || payload.EventCode == EventCodes.SECURITY_OTP))
                {
                    var fallbackChannel = channel == ChannelType.TELEGRAM ? ChannelType.EMAIL
                                        : channel == ChannelType.ZALO_ZNS ? ChannelType.EMAIL
                                        : ChannelType.WEB_PUSH;

                    _logger.LogWarning("[Smart Fallback Triggered] Sự cố kênh {Primary}. Kích hoạt kênh dự phòng khẩn cấp: {Fallback}!",
                        channel, fallbackChannel);

                    var fallbackStrategy = _strategyFactory.GetStrategy(fallbackChannel);
                    if (fallbackStrategy != null)
                    {
                        try
                        {
                            var fallbackRes = await fallbackStrategy.ExecuteAsync(context, ct);
                            if (fallbackRes.Status == DeliveryStatus.SENT || fallbackRes.Status == DeliveryStatus.DELIVERED)
                            {
                                payload.TraceLogs.Add(new TraceLogItem
                                {
                                    Step = $"Smart Fallback: {channel} -> {fallbackChannel} SUCCESS",
                                    Timestamp = DateTime.UtcNow,
                                    DurationMs = 15
                                });

                                result = new ChannelDeliveryResult
                                {
                                    Channel = channel,
                                    Status = DeliveryStatus.FALLBACK_SUCCESS,
                                    Recipient = $"{recipient} -> Fallback:{fallbackChannel}",
                                    ErrorMessage = $"Fallback kích hoạt thành công qua kênh {fallbackChannel}",
                                    Timestamp = DateTime.UtcNow
                                };
                            }
                        }
                        catch (Exception fbEx)
                        {
                            _logger.LogError(fbEx, "Kênh dự phòng {Fallback} cũng thất bại.", fallbackChannel);
                        }
                    }
                }

                // Cập nhật nguyên tử trạng thái của kênh tương ứng trong MongoDB
                var filter = Builders<NotificationDocument>.Filter.Eq(x => x.NotificationId, payload.NotificationId);
                var update = Builders<NotificationDocument>.Update
                    .Set("channels_status.$[elem].status", result.Status.ToString())
                    .Set("channels_status.$[elem].recipient", result.Recipient)
                    .Set("channels_status.$[elem].error_message", result.ErrorMessage)
                    .Set("channels_status.$[elem].timestamp", DateTime.UtcNow);

                var updateArrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("elem.channel", channel.ToString()))
                };

                await _collection.UpdateOneAsync(filter, update, new UpdateOptions { ArrayFilters = updateArrayFilters }, ct);
                _logger.LogInformation("Updated MongoDB status for Channel {Channel} -> {Status}", channel, result.Status);
            });

            await Task.WhenAll(tasks);

            // 8. Nhật ký truy vết Bước 4: Hoàn thành phát tán toàn bộ các kênh
            var totalDuration = (long)(DateTime.UtcNow - payload.CreatedAt).TotalMilliseconds;
            if (totalDuration < 1) totalDuration = 1;

            var step4Log = new TraceLogItem
            {
                Step = "4. All Channels Delivered",
                Timestamp = DateTime.UtcNow,
                DurationMs = totalDuration
            };

            payload.TraceLogs.Add(step4Log);

            // Lưu trữ toàn bộ nhật ký truy vết TraceLogs vào MongoDB
            var traceFilter = Builders<NotificationDocument>.Filter.Eq(x => x.NotificationId, payload.NotificationId);
            var traceUpdate = Builders<NotificationDocument>.Update.Set(x => x.TraceLogs, payload.TraceLogs);
            await _collection.UpdateOneAsync(traceFilter, traceUpdate, cancellationToken: ct);

            _logger.LogInformation("Finished multi-channel dispatch for NotificationId: {Id} (Total end-to-end: {Ms}ms)",
                payload.NotificationId, totalDuration);
        }

        private async Task PushToDlqAsync(NotificationPayload payload, ChannelType channel, string recipient, string errorMessage, CancellationToken ct)
        {
            try
            {
                var dlqPayload = JsonSerializer.Serialize(new
                {
                    NotificationId = payload.NotificationId,
                    UserId = payload.UserId,
                    EventCode = payload.EventCode,
                    Channel = channel.ToString(),
                    Recipient = recipient,
                    ErrorMessage = errorMessage,
                    FailedAt = DateTime.UtcNow,
                    Payload = payload
                }, _jsonOptions);

                await _dlqProducer.ProduceAsync(KafkaTopics.NotificationDlq, new Message<string, string>
                {
                    Key = payload.NotificationId.ToString(),
                    Value = dlqPayload
                }, ct);

                _logger.LogWarning("[Dead-Letter Queue] Forwarded failed notification {Id} ({Channel}) to Kafka topic '{DlqTopic}'",
                    payload.NotificationId, channel, KafkaTopics.NotificationDlq);
            }
            catch (Exception dlqEx)
            {
                _logger.LogError(dlqEx, "Failed to produce to DLQ Kafka topic for Notification {Id}", payload.NotificationId);
            }
        }

        private static string GetRecipientForChannel(ChannelType channel, NotificationPayload payload) =>
            channel switch
            {
                ChannelType.EMAIL => payload.Email ?? "ltd28112005@gmail.com",
                ChannelType.TELEGRAM => payload.TelegramChatId?.ToString() ?? "1547761725",
                ChannelType.ZALO_ZNS => payload.PhoneNumber ?? "84766408268",
                ChannelType.WEB_PUSH => "Windows_ActionCenter:" + payload.UserId,
                ChannelType.WEBHOOK => payload.WebhookUrl ?? "Discord Webhook",
                _ => $"user_{payload.UserId}"
            };

        public async Task DispatchReadEventAsync(NotificationReadPayload payload, CancellationToken ct)
        {
            _logger.LogInformation("Processing NotificationReadEvent for NotificationId: {Id}", payload.NotificationId);
            var filter = Builders<NotificationDocument>.Filter.Eq(x => x.NotificationId, payload.NotificationId);
            var update = Builders<NotificationDocument>.Update
                .Set(x => x.IsRead, true)
                .Set(x => x.ReadAt, payload.ReadAt);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
            _logger.LogInformation("Marked notification {Id} as read in MongoDB", payload.NotificationId);
        }

        public static string RenderTemplate(string template, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            // 1. Chuẩn hóa triệt để ký tự xuống dòng từ SQL Server / JSON escape
            var result = template
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\r\n", "\n");

            // 2. Thay thế các biến tham số nghiệp vụ
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    result = result.Replace("{{" + kvp.Key + "}}", kvp.Value);
                }
            }

            return result;
        }
    }
}
