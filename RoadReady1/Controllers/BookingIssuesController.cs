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
    public class BookingIssuesController : ControllerBase
    {
        private readonly IBookingIssueService _svc;
        private readonly IUserService _users; // 👈 add

        public BookingIssuesController(IBookingIssueService svc, IUserService users) // 👈 inject
        {
            _svc = svc;
            _users = users;
        }

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

        // Customer: create issue on their booking
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<BookingIssueDto>> Create(BookingIssueCreateDto dto)
        {
            try
            {
                var uid = await CurrentUserIdAsync();                // 👈 changed
                var created = await _svc.CreateAsync(uid, dto);
                return CreatedAtAction(nameof(GetMine), new { }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<BookingIssueDto>> GetMine()
            => await _svc.GetMineAsync(await CurrentUserIdAsync()); // 👈 changed

        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingIssueDto>> GetAll()
            => await _svc.GetAllAsync();

        [HttpGet("booking/{bookingId:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingIssueDto>> GetByBooking(int bookingId)
            => await _svc.GetByBookingAsync(bookingId);

        [HttpPatch("{issueId:int}/status")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> UpdateStatus(int issueId, BookingIssueStatusUpdateDto dto)
        {
            try
            {
                await _svc.UpdateStatusAsync(issueId, dto.Status);
                return NoContent();
            }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }
    }
}
