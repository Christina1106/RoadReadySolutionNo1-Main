// File: Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using RoadReady1.Exceptions;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
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
    }
}
