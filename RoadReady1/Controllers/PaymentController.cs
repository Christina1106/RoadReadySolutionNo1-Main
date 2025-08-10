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
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _svc;
        public PaymentsController(IPaymentService svc) => _svc = svc;

        private int CurrentUserId => int.Parse(User.FindFirstValue("uid")!);
        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Customer or Staff can trigger a payment
        [HttpPost]
        [Authorize(Roles = "Customer,Admin,RentalAgent")]
        public async Task<ActionResult<PaymentDto>> Pay(PaymentCreateDto dto)
        {
            try
            {
                var result = await _svc.PayAsync(CurrentUserId, CurrentRole, dto);
                return CreatedAtAction(nameof(GetById), new { id = result.PaymentId }, result);
            }
            catch (NotFoundException ex) { return NotFound(new { ex.Message }); }
            catch (UnauthorizedException ex) { return Forbid(ex.Message); }
            catch (BadRequestException ex) { return BadRequest(new { ex.Message }); }
        }

        // Customer: my payments
        [HttpGet("mine")]
        [Authorize(Roles = "Customer")]
        public async Task<IEnumerable<PaymentDto>> Mine()
            => await _svc.GetMineAsync(CurrentUserId);

        // Staff: all payments
        [HttpGet]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<IEnumerable<PaymentDto>> GetAll()
            => await _svc.GetAllAsync();

        // Staff: payment by id
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,RentalAgent")]
        public async Task<ActionResult<PaymentDto>> GetById(int id)
        {
            try { return Ok(await _svc.GetByIdAsync(id)); }
            catch (NotFoundException) { return NotFound(); }
        }
    }
}
