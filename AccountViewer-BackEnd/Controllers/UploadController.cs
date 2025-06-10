using AccountsViewer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccountsViewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] IFormFile file)
        {
            if (file == null)
                return BadRequest(new { Success = false, Message = "No file provided." });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _uploadService.ProcessFileAsync(file, currentUserId);

            if (!result.Success)
                return BadRequest(new { result.Success, result.Message });

            return Ok(result);
        }
    }
}
