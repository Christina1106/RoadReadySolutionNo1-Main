// File: Controllers/CarsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarsController : ControllerBase
    {
        private readonly ICarService _cars;
        public CarsController(ICarService cars) => _cars = cars;

        // Public list
        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<CarDto>> GetAll() => await _cars.GetAllAsync();

        // Public detail
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CarDto>> GetById(int id)
        {
            try { return Ok(await _cars.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }

        // Search availability (public)
        [HttpPost("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CarDto>>> Search(CarSearchRequestDto req)
        {
            try { return Ok(await _cars.SearchAsync(req)); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Quick availability check for a single car
        [HttpGet("{id:int}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability(int id, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc)
        {
            try
            {
                var ok = await _cars.IsAvailableAsync(id, fromUtc, toUtc);
                return ok ? NoContent() : Conflict(new { Message = "Not available for requested period." });
            }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Admin/Agent create
        [HttpPost]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<CarDto>> Create(CarCreateDto dto)
        {
            try
            {
                var created = await _cars.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.CarId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Admin/Agent update
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<CarDto>> Update(int id, CarUpdateDto dto)
        {
            try { return Ok(await _cars.UpdateAsync(id, dto)); }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Admin/Agent set status
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> SetStatus(int id, CarStatusUpdateDto dto)
        {
            try { await _cars.SetStatusAsync(id, dto.StatusId); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
        }

        // Admin/Agent delete
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _cars.DeleteAsync(id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
        }
    }
}
