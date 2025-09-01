using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using System.Security.Claims;
using RoadReady1.Filters;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [CustomExceptionFilter]
    [EnableCors("DefaultCORS")]
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly IMaintenanceRequestService _svc;
        private readonly IUserService _users; // 👈 add

        public MaintenanceRequestsController(IMaintenanceRequestService svc, IUserService users) // 👈 inject
        {
            _svc = svc;
            _users = users;
        }

        // resolve current user id from email/sub in token
        private async Task<int> CurrentUserIdAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                       ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedException("Token missing email/sub claim.");

            var me = await _users.GetByEmailAsync(email);
            if (me is null) throw new UnauthorizedException("User not found.");
            return me.UserId;
        }

        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Create (Customer or Staff)
        [HttpPost]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<ActionResult<MaintenanceRequestDto>> Create(MaintenanceRequestCreateDto dto)
        {
            try
            {
                var uid = await CurrentUserIdAsync();                             // 👈 changed
                var created = await _svc.CreateAsync(uid, CurrentRole, dto);      // 👈 same service API
                return CreatedAtAction(nameof(GetById), new { id = created.RequestId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPatch("{id:int}/resolve")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> Resolve(int id)
        {
            try { await _svc.ResolveAsync(id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("open")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> Open()
            => await _svc.GetOpenAsync();

        [HttpGet("car/{carId:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> ForCar(int carId)
            => await _svc.GetByCarAsync(carId);

        [HttpGet("mine")]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> Mine()
            => await _svc.GetMineAsync(await CurrentUserIdAsync());             // 👈 changed

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<MaintenanceRequestDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }
    }
}
