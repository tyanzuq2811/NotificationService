using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QueryService.Hubs;

namespace QueryService.Services
{
    public class InAppPushRelayWorker : BackgroundService
    {
        private readonly ILogger<InAppPushRelayWorker> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly string _groupId;

        public InAppPushRelayWorker(
            ILogger<InAppPushRelayWorker> logger,
            IHubContext<NotificationHub> hubContext,
            IConfiguration config)
        {
            _logger = logger;
            _hubContext = hubContext;
            _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
            _topic = config["Kafka:InAppPushTopic"] ?? KafkaTopics.InAppPushEvents;
            _groupId = config["Kafka:ConsumerGroupId"] ?? "query-inapp-push-group";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield(); // non-blocking startup

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(_topic);
            _logger.LogInformation("InAppPushRelayWorker subscribed to Kafka topic: {Topic}", _topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult == null || consumeResult.Message == null) continue;

                    var json = consumeResult.Message.Value;
                    var pushMsg = JsonSerializer.Deserialize<InAppPushMessage>(json);

                    if (pushMsg != null)
                    {
                        var groupName = $"user_{pushMsg.UserId}";
                        _logger.LogInformation("Broadcasting In-App notification to SignalR Group {Group} for NotificationId {Id}",
                            groupName, pushMsg.NotificationId);

                        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", pushMsg, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing In-App Push event from Kafka.");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            try
            {
                consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while closing In-App Kafka consumer.");
            }
        }
    }
}
