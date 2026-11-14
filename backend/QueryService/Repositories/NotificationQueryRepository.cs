using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace QueryService.Repositories
{
    public class NotificationQueryRepository : INotificationQueryRepository
    {
        private readonly IMongoCollection<NotificationDocument> _collection;
        private readonly string _sqlConnStr;

        public NotificationQueryRepository(IMongoClient client, IConfiguration config)
        {
            var dbName = config["ConnectionStrings:MongoDbName"] ?? "read_db";
            var database = client.GetDatabase(dbName);
            _collection = database.GetCollection<NotificationDocument>("notifications");
            _sqlConnStr = config.GetConnectionString("SqlServer")!;
        }

        public async Task<(List<NotificationDocument> items, long totalCount)> GetNotificationsAsync(
            string userId, int page, int pageSize, bool? isRead)
        {
            var filterBuilder = Builders<NotificationDocument>.Filter;
            var filter = filterBuilder.Eq(x => x.UserId, userId);

            if (isRead.HasValue)
            {
                filter &= filterBuilder.Eq(x => x.IsRead, isRead.Value);
            }

            var totalCount = await _collection.CountDocumentsAsync(filter);

            var items = await _collection.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<long> GetUnreadCountAsync(string userId)
        {
            var filter = Builders<NotificationDocument>.Filter.Eq(x => x.UserId, userId) &
                         Builders<NotificationDocument>.Filter.Eq(x => x.IsRead, false);

            return await _collection.CountDocumentsAsync(filter);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var filter = Builders<NotificationDocument>.Filter.Eq(x => x.UserId, userId) &
                         Builders<NotificationDocument>.Filter.Eq(x => x.IsRead, false);

            var update = Builders<NotificationDocument>.Update
                .Set(x => x.IsRead, true)
                .Set(x => x.ReadAt, DateTime.UtcNow);

            await _collection.UpdateManyAsync(filter, update);
        }

        public async Task<NotificationDocument?> GetByIdAsync(Guid notificationId)
        {
            return await _collection.Find(x => x.NotificationId == notificationId).FirstOrDefaultAsync();
        }

        public async Task<object> GetStatisticsAsync()
        {
            var totalCount = await _collection.CountDocumentsAsync(Builders<NotificationDocument>.Filter.Empty);
            var unreadCount = await _collection.CountDocumentsAsync(Builders<NotificationDocument>.Filter.Eq(x => x.IsRead, false));
            var readCount = totalCount - unreadCount;

            // Sample channel breakdown
            var inAppCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.IN_APP.ToString() && cs.Status == DeliveryStatus.DELIVERED.ToString()));

            var emailCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.EMAIL.ToString() && cs.Status == DeliveryStatus.SENT.ToString()));

            var telegramCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.TELEGRAM.ToString() && cs.Status == DeliveryStatus.SENT.ToString()));

            return new
            {
                TotalNotifications = totalCount,
                ReadNotifications = readCount,
                UnreadNotifications = unreadCount,
                ChannelsDelivered = new
                {
                    InApp = inAppCount,
                    Email = emailCount,
                    Telegram = telegramCount
                }
            };
        }

        public async Task<object> GetMetricsAsync()
        {
            var totalNotifications = await _collection.CountDocumentsAsync(FilterDefinition<NotificationDocument>.Empty);
            
            var inAppCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.IN_APP.ToString() && (cs.Status == DeliveryStatus.DELIVERED.ToString() || cs.Status == DeliveryStatus.SENT.ToString())));

            var emailCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.EMAIL.ToString() && (cs.Status == DeliveryStatus.SENT.ToString() || cs.Status == DeliveryStatus.DELIVERED.ToString())));

            var telegramCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.TELEGRAM.ToString() && (cs.Status == DeliveryStatus.SENT.ToString() || cs.Status == DeliveryStatus.DELIVERED.ToString() || cs.Status == DeliveryStatus.FALLBACK_SUCCESS.ToString())));

            var zaloCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.ZALO_ZNS.ToString() && (cs.Status == DeliveryStatus.SENT.ToString() || cs.Status == DeliveryStatus.DELIVERED.ToString() || cs.Status == DeliveryStatus.FALLBACK_SUCCESS.ToString())));

            var webPushCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.WEB_PUSH.ToString() && (cs.Status == DeliveryStatus.SENT.ToString() || cs.Status == DeliveryStatus.DELIVERED.ToString())));

            var webhookCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Channel == ChannelType.WEBHOOK.ToString() && (cs.Status == DeliveryStatus.SENT.ToString() || cs.Status == DeliveryStatus.DELIVERED.ToString())));

            var dlqCount = await _collection.CountDocumentsAsync(
                Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                    cs => cs.Status == DeliveryStatus.FAILED_DLQ.ToString() || cs.Status == DeliveryStatus.FAILED.ToString()));

            long totalChannelSuccess = inAppCount + emailCount + telegramCount + zaloCount + webPushCount + webhookCount;
            long totalChannelAttempts = totalChannelSuccess + dlqCount;
            double successRate = totalChannelAttempts > 0 
                ? Math.Round(((double)totalChannelSuccess / totalChannelAttempts) * 100, 1) 
                : 100.0;

            var recentDocs = await _collection.Find(FilterDefinition<NotificationDocument>.Empty)
                .SortByDescending(x => x.CreatedAt)
                .Limit(50)
                .ToListAsync();

            double avgOutboxLatencyMs = 28.5;
            var outboxDurations = recentDocs
                .SelectMany(d => d.TraceLogs ?? Enumerable.Empty<TraceLogItem>())
                .Where(t => t.Step.Contains("Outbox Relay"))
                .Select(t => t.DurationMs)
                .ToList();

            if (outboxDurations.Count > 0)
            {
                avgOutboxLatencyMs = Math.Round(outboxDurations.Average(), 1);
            }

            return new
            {
                totalNotifications,
                successRate,
                avgOutboxLatencyMs,
                dlqCount,
                channelBreakdown = new
                {
                    inApp = inAppCount,
                    email = emailCount,
                    telegram = telegramCount,
                    zalo = zaloCount,
                    webPush = webPushCount,
                    webhook = webhookCount
                },
                statusBreakdown = new
                {
                    delivered = inAppCount + webPushCount + webhookCount,
                    sent = emailCount + telegramCount + zaloCount,
                    pending = Math.Max(0, totalNotifications * 6 - (totalChannelSuccess + dlqCount)),
                    failedDlq = dlqCount
                }
            };
        }

        public async Task<List<NotificationDocument>> GetDlqNotificationsAsync(int limit)
        {
            var filter = Builders<NotificationDocument>.Filter.ElemMatch(x => x.ChannelsStatus,
                cs => cs.Status == DeliveryStatus.FAILED_DLQ.ToString() || cs.Status == DeliveryStatus.FAILED.ToString());

            return await _collection.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<UserPreference>> GetPreferencesAsync(string userId)
        {
            using var connection = new SqlConnection(_sqlConnStr);
            var sql = "SELECT * FROM UserPreferences WHERE UserId = @UserId";
            var result = (await connection.QueryAsync<UserPreference>(sql, new { UserId = userId })).AsList();
            return result;
        }

        public async Task<List<NotificationEvent>> GetOutboxEventsAsync(int limit)
        {
            using var connection = new SqlConnection(_sqlConnStr);
            var sql = "SELECT TOP (@Limit) * FROM NotificationEvents ORDER BY CreatedAt DESC";
            var result = (await connection.QueryAsync<NotificationEvent>(sql, new { Limit = limit })).AsList();
            return result;
        }
    }
}
