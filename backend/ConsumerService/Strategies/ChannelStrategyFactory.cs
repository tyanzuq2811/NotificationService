using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace ConsumerService.Strategies
{
    public class ChannelStrategyFactory
    {
        private readonly Dictionary<ChannelType, INotificationChannelStrategy> _strategies;

        public ChannelStrategyFactory(IEnumerable<INotificationChannelStrategy> strategies)
        {
            _strategies = strategies.ToDictionary(s => s.ChannelType);
        }

        public INotificationChannelStrategy? GetStrategy(ChannelType channelType)
        {
            _strategies.TryGetValue(channelType, out var strategy);
            return strategy;
        }
    }
}
