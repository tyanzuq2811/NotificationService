using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using ConsumerService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsumerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly NotificationDispatcher _dispatcher;
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly string _groupId;

        public Worker(
            ILogger<Worker> logger,
            NotificationDispatcher dispatcher,
            IConfiguration config)
        {
            _logger = logger;
            _dispatcher = dispatcher;
            _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
            _topic = config["Kafka:NotificationEventsTopic"] ?? KafkaTopics.NotificationEvents;
            _groupId = config["Kafka:ConsumerGroupId"] ?? "notification-consumer-group";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield(); // ensure non-blocking startup

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

            // Subscribing to Standard, Critical Priority, and System Config Topics
            var subscribedTopics = new[]
            {
                _topic,
                KafkaTopics.NotificationEventsCritical,
                KafkaTopics.NotificationEventsStandard,
                KafkaTopics.SystemConfigEvents
            };

            consumer.Subscribe(subscribedTopics);
            _logger.LogInformation("ConsumerService subscribed to Kafka topics: [{Topics}] with GroupId: {GroupId}",
                string.Join(", ", subscribedTopics), _groupId);

            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? consumeResult = null;
                try
                {
                    consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult == null || consumeResult.Message == null) continue;

                    var topic = consumeResult.Topic;
                    var json = consumeResult.Message.Value;

                    if (topic == KafkaTopics.SystemConfigEvents)
                    {
                        _logger.LogInformation("[Kafka Event] Received system config event on topic {Topic}", topic);
                        _dispatcher.InvalidateTemplatesCache();
                        consumer.Commit(consumeResult);
                        continue;
                    }

                    _logger.LogInformation("Received Kafka event on topic {Topic}, Partition {Partition}, Offset {Offset}",
                        consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

                    // Poison Pill Protection: Validate and Parse JSON
                    string? eventType = null;
                    string? payloadJson = null;

                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        eventType = root.GetProperty("EventType").GetString();
                        payloadJson = root.GetProperty("Payload").GetString();
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "[Poison Pill Detected] Tin nhắn Kafka không đúng chuẩn JSON tại Offset {Offset}. Chuyển vào DLQ ngay!",
                            consumeResult.Offset.Value);

                        await _dispatcher.SendPoisonPillToDlqAsync(json, $"Malformed JSON: {jsonEx.Message}", stoppingToken);
                        consumer.Commit(consumeResult); // Commit to unblock topic
                        continue;
                    }

                    if (string.IsNullOrEmpty(payloadJson))
                    {
                        consumer.Commit(consumeResult);
                        continue;
                    }

                    if (eventType == EventTypes.NotificationRequested)
                    {
                        NotificationPayload? payload = null;
                        try
                        {
                            payload = JsonSerializer.Deserialize<NotificationPayload>(payloadJson);
                        }
                        catch (JsonException pEx)
                        {
                            _logger.LogError(pEx, "[Poison Pill Payload] Lỗi deserialize NotificationPayload tại Offset {Offset}. Chuyển DLQ!",
                                consumeResult.Offset.Value);

                            await _dispatcher.SendPoisonPillToDlqAsync(payloadJson, $"Malformed Payload: {pEx.Message}", stoppingToken);
                            consumer.Commit(consumeResult);
                            continue;
                        }

                        if (payload != null)
                        {
                            await _dispatcher.DispatchRequestedEventAsync(payload, stoppingToken);
                        }
                    }
                    else if (eventType == EventTypes.NotificationRead)
                    {
                        var payload = JsonSerializer.Deserialize<NotificationReadPayload>(payloadJson);
                        if (payload != null)
                        {
                            await _dispatcher.DispatchReadEventAsync(payload, stoppingToken);
                        }
                    }

                    consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message in ConsumerService.");
                    if (consumeResult != null)
                    {
                        try { consumer.Commit(consumeResult); } catch { }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
