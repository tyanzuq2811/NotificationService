using System;
using System.Threading.Tasks;
using CommandService.Models;
using CommandService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationCommandService _commandService;

        public NotificationsController(INotificationCommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new CommandResponse { Success = false, Message = "UserId is required" });
            }

            var notificationId = await _commandService.SendNotificationAsync(request);

            return Accepted(new CommandResponse
            {
                Success = true,
                Message = "Notification queued for delivery via Transactional Outbox",
                Data = new { notificationId }
            });
        }

        [HttpPost("send-bulk")]
        public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequest request)
        {
            if (request.UserIds == null || request.UserIds.Count == 0)
            {
                return BadRequest(new CommandResponse { Success = false, Message = "UserIds list must not be empty" });
            }

            var notificationIds = await _commandService.SendBulkNotificationAsync(request);

            return Accepted(new CommandResponse
            {
                Success = true,
                Message = $"Bulk notifications queued for {notificationIds.Count} users via Transactional Outbox",
                Data = new { total = notificationIds.Count, notificationIds }
            });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, [FromBody] MarkAsReadRequest request)
        {
            await _commandService.MarkAsReadAsync(id, request.UserId);

            return Accepted(new CommandResponse
            {
                Success = true,
                Message = "Notification read event queued",
                Data = new { notificationId = id }
            });
        }
    }
}
