// File: Controllers/BookingIssuesController.cs
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
        public BookingIssuesController(IBookingIssueService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);

        // Customer: create issue on their booking
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<BookingIssueDto>> Create(BookingIssueCreateDto dto)
        {
            try
            {
                var created = await _svc.CreateAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetMine), new { }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Customer: list own issues
        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<BookingIssueDto>> GetMine()
            => await _svc.GetMineAsync(CurrentUserId);

        // Staff: list all issues
        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingIssueDto>> GetAll()
            => await _svc.GetAllAsync();

        // Staff: list issues for a booking
        [HttpGet("booking/{bookingId:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<BookingIssueDto>> GetByBooking(int bookingId)
            => await _svc.GetByBookingAsync(bookingId);

        // Staff: update status
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
