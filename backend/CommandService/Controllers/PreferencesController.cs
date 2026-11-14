using System.Threading.Tasks;
using CommandService.Models;
using CommandService.Services;
using Common;
using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreferencesController : ControllerBase
    {
        private readonly INotificationCommandService _commandService;

        public PreferencesController(INotificationCommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPreferences(string userId)
        {
            var preferences = await _commandService.GetPreferencesAsync(userId);
            return Ok(new CommandResponse
            {
                Success = true,
                Message = "User preferences fetched successfully",
                Data = preferences
            });
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdatePreference(string userId, [FromBody] UserPreference preference)
        {
            preference.UserId = userId;
            await _commandService.UpdatePreferenceAsync(preference);

            return Ok(new CommandResponse
            {
                Success = true,
                Message = "User preference updated successfully",
                Data = preference
            });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly INotificationCommandService _commandService;

        public AdminController(INotificationCommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpGet("outbox-events")]
        public async Task<IActionResult> GetOutboxEvents([FromQuery] int limit = 50)
        {
            var events = await _commandService.GetOutboxEventsAsync(limit);
            return Ok(new CommandResponse
            {
                Success = true,
                Message = "Outbox events fetched successfully",
                Data = events
            });
        }

        [HttpPost("dlq/{id}/retry")]
        [HttpPost("/api/admin/dlq/{id}/retry")]
        public async Task<IActionResult> RetryDlq(System.Guid id)
        {
            var success = await _commandService.RetryDlqNotificationAsync(id);
            if (!success)
            {
                return NotFound(new CommandResponse { Success = false, Message = "Notification not found or not in retryable state" });
            }
            return Ok(new CommandResponse
            {
                Success = true,
                Message = $"Notification {id} re-queued for delivery from DLQ",
                Data = new { notificationId = id }
            });
        }
    }
}
