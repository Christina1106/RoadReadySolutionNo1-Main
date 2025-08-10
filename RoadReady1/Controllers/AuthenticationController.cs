using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPasswordService _passwordService;

        public AuthenticationController(IAuthService authService, IPasswordService passwordService)
        {
            _authService = authService;
            _passwordService = passwordService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Created("", null);
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(new { token });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            await _passwordService.RequestPasswordResetAsync(dto);
            return Ok(new { Message = "If the email exists, you'll receive a reset link." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            try
            {
                await _passwordService.ResetPasswordAsync(dto);
                return Ok(new { Message = "Password reset successful." });
            }
            catch (InvalidTokenException ex)
            {
                return BadRequest(new { ex.Message });
            }
        }
    }
}
