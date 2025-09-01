// File: Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Models;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using RoadReady1.Filters;
using System.Security.Claims;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    [CustomExceptionFilter]
    [EnableCors("DefaultCORS")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }


        // inside RoadReady1.Controllers.UsersController
        [HttpGet("me")]
        public async Task<ActionResult<UserMeDto>> Me()
        {
            // Prefer Email/NameIdentifier; fall back to "sub"
            var email = User.FindFirstValue(ClaimTypes.Email)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "Token does not include an email/sub." });

            var me = await _userService.GetByEmailAsync(email);
            if (me == null) return NotFound();

            return Ok(new UserMeDto
            {
                UserId = me.UserId,
                FirstName = me.FirstName,
                LastName = me.LastName,
                Email = me.Email,
                RoleName = me.Role?.RoleName
            });
        }

        /// <summary>
        /// Create a new user. Requires a valid JWT.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Create([FromBody] UserCreateDto dto)
        {
            var created = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
        }

        // Admins list all users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IEnumerable<UserDto>> GetAll() => await _userService.GetAllAsync();

        // Admins get any user by id
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                return Ok(user);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        // Admins update users
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var updated = await _userService.UpdateAsync(id, dto);
                return Ok(updated);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        // Admins delete users
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        // PATCH: api/Users/5/role
        [HttpPatch("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            if (dto is null || (dto.RoleId is null && string.IsNullOrWhiteSpace(dto.RoleName)))
                return BadRequest(new { message = "Provide roleId or roleName." });

            try
            {
                await _userService.ChangeRoleAsync(id, dto.RoleId, dto.RoleName);
                return NoContent();
            }
            catch (NotFoundException) { return NotFound(); }
        }

        // PATCH: api/Users/5/status
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetUserStatusDto dto)
        {
            try
            {
                await _userService.SetActiveAsync(id, dto.IsActive);
                return NoContent();
            }
            catch (NotFoundException) { return NotFound(); }
        }

        // DTOs for the above endpoints
        public record ChangeRoleDto(int? RoleId, string? RoleName);
        public record SetUserStatusDto(bool IsActive);


    }
}
