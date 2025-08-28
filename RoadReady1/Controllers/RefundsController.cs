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

    public class RefundsController : ControllerBase
    {
        private readonly IRefundService _svc;
        public RefundsController(IRefundService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);

        // Customer: request a refund
        [HttpPost("request")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<RefundDto>> RequestRefund(RefundRequestCreateDto dto)
        {
            try
            {
                var created = await _svc.RequestAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetMyRefunds), new { }, created);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Customer: my refund requests
        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<RefundDto>> GetMyRefunds()
            => await _svc.MineAsync(CurrentUserId);

        // Staff: list all
        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<RefundDto>> GetAll()
            => await _svc.GetAllAsync();

        // Staff: detail
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<RefundDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }

        // Staff: approve
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> Approve(int id)
        {
            try { await _svc.ApproveAsync(id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Staff: reject
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IActionResult> Reject(int id)
        {
            try { await _svc.RejectAsync(id); return NoContent(); }
            catch (NotFoundException) { return NotFound(); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }
    }
}

