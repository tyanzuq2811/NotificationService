using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;

namespace QueryService.Repositories
{
    public interface INotificationQueryRepository
    {
        Task<(List<NotificationDocument> items, long totalCount)> GetNotificationsAsync(string userId, int page, int pageSize, bool? isRead);
        Task<long> GetUnreadCountAsync(string userId);
        Task MarkAllAsReadAsync(string userId);
        Task<NotificationDocument?> GetByIdAsync(Guid notificationId);
        Task<object> GetStatisticsAsync();
        Task<object> GetMetricsAsync();
        Task<List<NotificationDocument>> GetDlqNotificationsAsync(int limit);
        Task<List<UserPreference>> GetPreferencesAsync(string userId);
        Task<List<NotificationEvent>> GetOutboxEventsAsync(int limit);
    }
}
