using System.Threading.Tasks;
using CommandService.Models;
using CommandService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebPushController : ControllerBase
    {
        private readonly INotificationCommandService _commandService;
        // Khóa công khai VAPID chuẩn cho trình duyệt phía Client (khớp với cặp khóa bảo mật VAPID)
        public const string VapidPublicKey = "BNjt2n6zUqd5yS9ORdDmRNxr16jH07LBTxdFjlj8m0eb167uR3uRC-uD_EAUean3NqbOoFTdU-uOzN1FiqQIf5E";

        public WebPushController(INotificationCommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpGet("public-key")]
        public IActionResult GetPublicKey()
        {
            return Ok(new CommandResponse
            {
                Success = true,
                Message = "VAPID Public Key fetched successfully",
                Data = new { publicKey = VapidPublicKey }
            });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] WebPushSubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Endpoint) || string.IsNullOrWhiteSpace(request.P256dh))
            {
                return BadRequest(new CommandResponse { Success = false, Message = "Invalid subscription data" });
            }

            await _commandService.SaveWebPushSubscriptionAsync(request);

            return Ok(new CommandResponse
            {
                Success = true,
                Message = $"WebPush subscription saved successfully for User {request.UserId}",
                Data = new { userId = request.UserId }
            });
        }
    }
}
