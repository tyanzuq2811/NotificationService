using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsumerService.Strategies
{
    public class InAppChannelStrategy : INotificationChannelStrategy
    {
        public ChannelType ChannelType => ChannelType.IN_APP;

        private readonly IProducer<string, string> _producer;
        private readonly string _inAppTopic;
        private readonly ILogger<InAppChannelStrategy> _logger;

        public InAppChannelStrategy(IConfiguration config, ILogger<InAppChannelStrategy> logger)
        {
            _logger = logger;
            _inAppTopic = config["Kafka:InAppPushTopic"] ?? KafkaTopics.InAppPushEvents;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
                Acks = Acks.Leader
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task<ChannelDeliveryResult> ExecuteAsync(NotificationContext context, CancellationToken ct)
        {
            try
            {
                var pushMsg = new InAppPushMessage
                {
                    NotificationId = context.NotificationId,
                    UserId = context.UserId,
                    EventCode = context.EventCode,
                    Title = context.Title,
                    Content = context.Content,
                    CreatedAt = DateTime.UtcNow
                };

                var message = new Message<string, string>
                {
                    Key = context.UserId,
                    Value = JsonSerializer.Serialize(pushMsg)
                };

                await _producer.ProduceAsync(_inAppTopic, message, ct);
                _logger.LogInformation("[In-App Channel] Dispatched WebSocket push event for User {UserId}", context.UserId);

                return ChannelDeliveryResult.Success(ChannelType.IN_APP, DeliveryStatus.DELIVERED, $"SignalR:user_{context.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[In-App Channel] Error dispatching push event for User {UserId}", context.UserId);
                return ChannelDeliveryResult.Failed(ChannelType.IN_APP, context.UserId, ex.Message);
            }
        }
    }
}
