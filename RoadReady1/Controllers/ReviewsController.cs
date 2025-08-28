using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models.DTOs;
using RoadReady1.Filters;
using System.Security.Claims;

namespace RoadReady1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    [CustomExceptionFilter]
    [EnableCors("DefaultCORS")]

    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _svc;
        public ReviewsController(IReviewService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);
        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Anyone can see car reviews (handy for car detail page)
        [HttpGet("car/{carId:int}")]
        [AllowAnonymous]
        public async Task<IEnumerable<ReviewDto>> GetForCar(int carId)
            => await _svc.GetByCarAsync(carId);

        // Customer: my reviews
        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<ReviewDto>> Mine()
            => await _svc.GetMineAsync(CurrentUserId);

        // Create (Customer only)
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<ReviewDto>> Create(ReviewCreateDto dto)
        {
            try
            {
                var created = await _svc.CreateAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ReviewId }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Get by id (staff convenience/debug)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<ReviewDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }

        // Update (owner only)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<ReviewDto>> Update(int id, ReviewUpdateDto dto)
        {
            try { return Ok(await _svc.UpdateAsync(CurrentUserId, id, dto)); }
            catch (NotFoundException) { return NotFound(); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Delete (owner or staff)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _svc.DeleteAsync(CurrentUserId, CurrentRole, id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
        }
    }
}
