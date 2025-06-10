using Microsoft.AspNetCore.Mvc;
using AccountsViewer.Services.Interfaces;
using AccountsViewer.DTOs;

namespace AccountsViewer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto creds)
        {
            var result = await _auth.AuthenticateAsync(creds.Username, creds.Password);
            if (result == null)
                return Unauthorized(new { error = "Invalid credentials" });

            return Ok(result);
        }
    }
}
