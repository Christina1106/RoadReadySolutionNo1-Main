using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        /// Public signup. Honors RoleId from body (1=Admin, 2=RentalAgent, 3=Customer).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _authService.RegisterAsync(dto);
                // include what the client asked for so you can verify quickly in Swagger
                return Created("", new { created.UserId, created.Email, created.RoleName, requestedRoleId = dto.RoleId});
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex) // invalid RoleId or role not found
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin-only signup. Honors the RoleId supplied in the body (1/2/3).
        /// Requires an Admin JWT.
        /// </summary>
        [HttpPost("register-by-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterByAdmin([FromBody] UserRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _authService.RegisterWithRoleAsync(dto);
                return Created("", new { created.UserId, created.Email, created.RoleName, requestedRoleId = dto.RoleId });
            }
            catch (UserAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        [HttpGet("whoami")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult WhoAmI()
            => Ok(User.Claims.Select(c => new { c.Type, c.Value }));

        [HttpGet("require-customer")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Customer")]
        public IActionResult OnlyCustomer() => Ok("You are a Customer.");
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
