using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Filters;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("DefaultCORS")]
    [CustomExceptionFilter]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Public signup. Always creates a Customer (RoleId = 3).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            try
            {
                var created = await _authService.RegisterAsync(dto);
                // return minimal Created payload (avoid exposing hashes etc.)
                return Created("", new { created.UserId, created.Email, created.RoleName });
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin-only signup. Honors the RoleId supplied in the body (1/2/3).
        /// Use Swagger "Authorize" with an Admin JWT first.
        /// </summary>
        [HttpPost("register-by-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterByAdmin([FromBody] UserRegisterDto dto)
        {
            try
            {
                var created = await _authService.RegisterWithRoleAsync(dto);
                return Created("", new { created.UserId, created.Email, created.RoleName });
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
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
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}


        //[HttpPost("forgot-password")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        //{
        //    await _passwordService.RequestPasswordResetAsync(dto);
        //    return Ok(new { Message = "If the email exists, you'll receive a reset link." });
        //}

        //[HttpPost("reset-password")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        //{
        //    try { await _passwordService.ResetPasswordAsync(dto); return Ok(new { Message = "Password reset successful." }); }
        //    catch (InvalidTokenException ex) 
        //    { 
        //        return BadRequest(new { ex.Message });
        //    }
        //}
