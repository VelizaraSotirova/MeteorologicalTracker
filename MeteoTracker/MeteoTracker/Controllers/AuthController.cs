using Microsoft.AspNetCore.Mvc;
using MeteoTracker.DTOs;
using MeteoTracker.Services;
using System.Threading.Tasks;

namespace MeteoTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(new { Message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                return Unauthorized(result.Message);
            }

            return Ok(new { Token = result.Token, Role = result.Role, Username = dto.Username });
        }
    }
}