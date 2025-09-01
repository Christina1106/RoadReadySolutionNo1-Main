using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Filters;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using RoadReady1.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;


namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("DefaultCORS")]
    [CustomExceptionFilter]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // 👈 force JWT bearer for every action here
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _svc;
        public BookingsController(IBookingService svc) => _svc = svc;

        // safer: try to get uid as int from claims (don't throw if missing)
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            // Primary claim we issue
            var uid = User.FindFirstValue("uid");
            // Optional fallbacks if you ever change your token
            uid ??= User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(uid, out userId);
        }

        // --- Public quote ---
        [HttpPost("quote")]
        [AllowAnonymous] // explicitly allow unauthenticated calls
        public async Task<ActionResult<BookingQuoteDto>> Quote([FromBody] BookingQuoteRequestDto req)
        {
            try { return Ok(await _svc.QuoteAsync(req)); }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // --- Customer create ---
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<BookingDto>> Create([FromBody] BookingCreateDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(); // or Forbid()

            try
            {
                var created = await _svc.CreateAsync(userId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.BookingId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // --- Customer mine (add "my" alias to match FE) ---
        [HttpGet("mine")]
        [HttpGet("my")] // 👈 alias so /api/Bookings/my works
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> Mine()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var items = await _svc.GetMineAsync(userId);
            return Ok(items);
        }

        // --- Staff list ---
        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingDto>> GetAll()
            => await _svc.GetAllAsync();

        // --- Staff detail ---
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<BookingDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }

        // --- Customer cancel ---
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            try
            {
                await _svc.CancelAsync(userId, id);
                return NoContent();
            }
            catch (NotFoundException) { return NotFound(); }
            catch (UnauthorizedException) { return Forbid(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }
    }

}
