using System.Threading.Tasks;
using CommandService.Models;
using CommandService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplatesController : ControllerBase
    {
        private readonly INotificationCommandService _commandService;

        public TemplatesController(INotificationCommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _commandService.GetAllTemplatesAsync();
            return Ok(new CommandResponse
            {
                Success = true,
                Message = "Templates fetched successfully",
                Data = templates
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTemplateRequest request)
        {
            var updated = await _commandService.UpdateTemplateAsync(id, request);
            if (!updated)
            {
                return NotFound(new CommandResponse { Success = false, Message = "Template not found" });
            }
            return Ok(new CommandResponse
            {
                Success = true,
                Message = "Template updated successfully and cache invalidated via Kafka",
                Data = new { templateId = id }
            });
        }
    }
}
