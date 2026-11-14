using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommandService.Models;
using Common;

namespace CommandService.Services
{
    public interface INotificationCommandService
    {
        Task<Guid> SendNotificationAsync(SendNotificationRequest request);
        Task<List<Guid>> SendBulkNotificationAsync(SendBulkNotificationRequest request);
        Task MarkAsReadAsync(Guid notificationId, string userId);
        Task<List<UserPreference>> GetPreferencesAsync(string userId);
        Task UpdatePreferenceAsync(UserPreference preference);
        Task<List<NotificationEvent>> GetOutboxEventsAsync(int limit = 50);
        Task<List<NotificationTemplate>> GetAllTemplatesAsync();
        Task<bool> UpdateTemplateAsync(int id, UpdateTemplateRequest request);
        Task<bool> RetryDlqNotificationAsync(Guid notificationId);
        Task SaveWebPushSubscriptionAsync(WebPushSubscriptionRequest request);
        Task<WebPushSubscriptionItem?> GetWebPushSubscriptionAsync(string userId);
    }
}
