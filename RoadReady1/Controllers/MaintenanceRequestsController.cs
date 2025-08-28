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
        public MaintenanceRequestsController(IMaintenanceRequestService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);
        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Create (Customer or Staff)
        [HttpPost]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<ActionResult<MaintenanceRequestDto>> Create(MaintenanceRequestCreateDto dto)
        {
            try
            {
                var created = await _svc.CreateAsync(CurrentUserId, CurrentRole, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.RequestId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Staff: resolve
        [HttpPatch("{id:int}/resolve")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> Resolve(int id)
        {
            try { await _svc.ResolveAsync(id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Staff: open requests
        [HttpGet("open")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> Open()
            => await _svc.GetOpenAsync();

        // Staff: by car
        [HttpGet("car/{carId:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> ForCar(int carId)
            => await _svc.GetByCarAsync(carId);

        // Reporter: my requests
        [HttpGet("mine")]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<IEnumerable<MaintenanceRequestDto>> Mine()
            => await _svc.GetMineAsync(CurrentUserId);

        // Staff: detail
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<MaintenanceRequestDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }
    }
}
