using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using System.Security.Claims;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _svc;
        public BookingsController(IBookingService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);

        // Public quote
        [HttpPost("quote")]
        [AllowAnonymous]
        public async Task<ActionResult<BookingQuoteDto>> Quote(BookingQuoteRequestDto req)
        {
            try { return Ok(await _svc.QuoteAsync(req)); }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Customer create
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<BookingDto>> Create(BookingCreateDto dto)
        {
            try
            {
                var created = await _svc.CreateAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.BookingId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Customer mine
        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<BookingDto>> Mine()
            => await _svc.GetMineAsync(CurrentUserId);

        // Staff list
        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingDto>> GetAll()
            => await _svc.GetAllAsync();

        // Staff detail
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<BookingDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }

        // Customer cancel
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _svc.CancelAsync(CurrentUserId, id);
                return NoContent();
            }
            catch (NotFoundException) { return NotFound(); }
            catch (UnauthorizedException) { return Forbid(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }
    }
}
