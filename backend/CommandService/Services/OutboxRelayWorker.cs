using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommandService.Services
{
    public class OutboxRelayWorker : BackgroundService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string _connectionString;
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly ILogger<OutboxRelayWorker> _logger;
        private IProducer<string, string>? _producer;

        public OutboxRelayWorker(IConfiguration config, ILogger<OutboxRelayWorker> logger)
        {
            _connectionString = config.GetConnectionString("SqlServer")!;
            _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
            _topic = config["Kafka:NotificationEventsTopic"] ?? KafkaTopics.NotificationEvents;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 5000
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _logger.LogInformation("OutboxRelayWorker initialized Kafka Producer to {Servers}", _bootstrapServers);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxRelayWorker started monitoring pending outbox events (UPDLOCK, READPAST, Priority Lanes)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingEventsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in OutboxRelayWorker loop.");
                }

                await Task.Delay(500, stoppingToken);
            }
        }

        private async Task ProcessPendingEventsAsync(CancellationToken stoppingToken)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(stoppingToken);

            // Enterprise Row-level lock hints (UPDLOCK, READPAST, ROWLOCK) to prevent multi-instance race conditions
            var selectSql = @"
                SELECT TOP 50 * FROM NotificationEvents WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE IsPublished = 0 AND (ScheduledAt IS NULL OR ScheduledAt <= GETUTCDATE())
                ORDER BY CreatedAt ASC";

            var pendingEvents = (await connection.QueryAsync<NotificationEvent>(selectSql)).AsList();

            if (pendingEvents.Count == 0) return;

            _logger.LogInformation("OutboxRelayWorker locked {Count} pending events to publish.", pendingEvents.Count);

            foreach (var evt in pendingEvents)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var relayDuration = (long)(DateTime.UtcNow - evt.CreatedAt).TotalMilliseconds;
                    if (relayDuration < 1) relayDuration = 1;

                    try
                    {
                        var p = JsonSerializer.Deserialize<NotificationPayload>(evt.Payload);
                        if (p != null)
                        {
                            p.TraceLogs ??= new List<TraceLogItem>();
                            p.TraceLogs.Add(new TraceLogItem
                            {
                                Step = "2. Outbox Relay -> Kafka",
                                Timestamp = DateTime.UtcNow,
                                DurationMs = relayDuration
                            });
                            evt.Payload = JsonSerializer.Serialize(p, _jsonOptions);
                        }
                    }
                    catch
                    {
                        // Payload might be NotificationReadPayload
                    }

                    var kafkaMessage = new Message<string, string>
                    {
                        Key = evt.AggregateId.ToString(),
                        Value = JsonSerializer.Serialize(new
                        {
                            evt.EventId,
                            evt.EventType,
                            evt.AggregateId,
                            evt.Payload,
                            evt.CreatedAt
                        }, _jsonOptions)
                    };

                    // Priority Topic Routing: Critical (OTP/Alert) vs Standard (Order/Balance)
                    var targetTopic = evt.Priority == "HIGH"
                        ? KafkaTopics.NotificationEventsCritical
                        : KafkaTopics.NotificationEventsStandard;

                    // Publish strictly to the designated priority topic to prevent duplicate consumption
                    var deliveryResult = await _producer!.ProduceAsync(targetTopic, kafkaMessage, stoppingToken);

                    if (deliveryResult.Status == PersistenceStatus.Persisted || deliveryResult.Status == PersistenceStatus.PossiblyPersisted)
                    {
                        var updateSql = @"
                            UPDATE NotificationEvents 
                            SET IsPublished = 1, PublishedAt = GETUTCDATE() 
                            WHERE EventId = @EventId";

                        await connection.ExecuteAsync(updateSql, new { evt.EventId });
                        _logger.LogInformation("Outbox event {EventId} successfully published to Kafka topic {Topic} and Priority Topic {PriorityTopic}",
                            evt.EventId, deliveryResult.Topic, targetTopic);
                    }
                }
                catch (ProduceException<string, string> pex)
                {
                    _logger.LogError(pex, "Kafka ProduceException for Outbox Event {EventId}: {Reason}",
                        evt.EventId, pex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error publishing Outbox Event {EventId}", evt.EventId);
                }
            }
        }

        public override void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
            base.Dispose();
        }
    }
}
