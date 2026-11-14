using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QueryService.Repositories;

namespace QueryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationQueryRepository _repository;

        public NotificationsController(INotificationQueryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] string userId = "user_demo",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isRead = null)
        {
            var (items, totalCount) = await _repository.GetNotificationsAsync(userId, page, pageSize, isRead);
            return Ok(new
            {
                success = true,
                message = "Notifications fetched successfully",
                data = items,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string userId = "user_demo")
        {
            var count = await _repository.GetUnreadCountAsync(userId);
            return Ok(new
            {
                success = true,
                unreadCount = count
            });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] string userId = "user_demo")
        {
            await _repository.MarkAllAsReadAsync(userId);
            return Ok(new
            {
                success = true,
                message = "All notifications marked as read"
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound(new { success = false, message = "Notification not found" });

            return Ok(new { success = true, data = item });
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var stats = await _repository.GetStatisticsAsync();
            return Ok(new { success = true, data = stats });
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            var metrics = await _repository.GetMetricsAsync();
            return Ok(new { success = true, data = metrics });
        }

        [HttpGet("dlq")]
        public async Task<IActionResult> GetDlqNotifications([FromQuery] int limit = 50)
        {
            var items = await _repository.GetDlqNotificationsAsync(limit);

            // Flatten failed channels so each DLQ row has explicit Channel, ErrorMessage, Recipient and FailedAt
            var dlqRows = new System.Collections.Generic.List<object>();
            foreach (var doc in items)
            {
                var failedChannels = doc.ChannelsStatus?.FindAll(cs =>
                    !string.IsNullOrEmpty(cs.Status) && (
                        cs.Status.IndexOf("FAIL", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cs.Status.IndexOf("DLQ", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        !string.IsNullOrEmpty(cs.ErrorMessage)
                    ));

                if (failedChannels != null && failedChannels.Count > 0)
                {
                    foreach (var cs in failedChannels)
                    {
                        dlqRows.Add(new
                        {
                            notificationId = doc.NotificationId,
                            userId = doc.UserId,
                            eventCode = doc.EventCode,
                            title = doc.Title,
                            channel = cs.Channel,
                            status = cs.Status,
                            recipient = cs.Recipient,
                            errorMessage = cs.ErrorMessage,
                            failedAt = cs.Timestamp,
                            createdAt = doc.CreatedAt
                        });
                    }
                }
                else
                {
                    dlqRows.Add(new
                    {
                        notificationId = doc.NotificationId,
                        userId = doc.UserId,
                        eventCode = doc.EventCode,
                        title = doc.Title,
                        channel = "UNKNOWN",
                        status = "FAILED_DLQ",
                        recipient = $"user_{doc.UserId}",
                        errorMessage = "Failed event in Dead-Letter Queue",
                        failedAt = doc.CreatedAt,
                        createdAt = doc.CreatedAt
                    });
                }
            }

            return Ok(new { success = true, data = dlqRows, total = dlqRows.Count });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PreferencesController : ControllerBase
    {
        private readonly INotificationQueryRepository _repository;

        public PreferencesController(INotificationQueryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPreferences(string userId)
        {
            var prefs = await _repository.GetPreferencesAsync(userId);
            return Ok(new
            {
                success = true,
                message = "User preferences fetched successfully",
                data = prefs
            });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly INotificationQueryRepository _repository;

        public AdminController(INotificationQueryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("outbox-events")]
        [HttpGet("outbox")]
        public async Task<IActionResult> GetOutboxEvents([FromQuery] int limit = 50)
        {
            var events = await _repository.GetOutboxEventsAsync(limit);
            return Ok(new
            {
                success = true,
                message = "Outbox events fetched successfully",
                data = events
            });
        }
    }
}
