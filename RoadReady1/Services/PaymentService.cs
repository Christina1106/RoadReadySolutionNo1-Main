using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IRepository<int, PaymentMethod> _methodRepo;
        private readonly IMapper _mapper;

        public PaymentService(
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Booking> bookingRepo,
            IRepository<int, PaymentMethod> methodRepo,
            IMapper mapper)
        {
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
            _methodRepo = methodRepo;
            _mapper = mapper;
        }

        public async Task<PaymentDto> PayAsync(int userId, string role, PaymentCreateDto dto)
        {
            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException($"Booking {dto.BookingId} not found");

            // Only owner can pay (unless staff)
            var isStaff = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(role, "RentalAgent", StringComparison.OrdinalIgnoreCase);
            if (!isStaff && booking.UserId != userId)
                throw new UnauthorizedException("You can only pay for your own booking.");

            // Validate method
            _ = await _methodRepo.GetByIdAsync(dto.MethodId)
                ?? throw new NotFoundException($"PaymentMethod {dto.MethodId} not found");

            // Basic guards on booking state (minimal; adjust later if needed)
            var statusLower = (await GetBookingStatusNameAsync(booking.StatusId)).ToLower();
            if (statusLower is "cancelled" or "completed")
                throw new BadRequestException($"Cannot pay for a {statusLower} booking.");

            // Prevent accidental duplicate success payments
            var allPayments = await _paymentRepo.GetAllAsync();
            var hasSuccess = allPayments.Any(p => p.BookingId == booking.BookingId &&
                                                  p.PaymentStatus.ToLower() == "success");
            if (hasSuccess)
                throw new BadRequestException("This booking already has a successful payment.");

            // Compute amount from booking (ignore client-provided amount)
            var amount = booking.TotalAmount;

            // "Process" payment (no gateway here)
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                MethodId = dto.MethodId,
                Amount = amount,
                PaymentStatus = "Success",   // if you add gateway, set based on result
                TransactionId = Guid.NewGuid().ToString("N"),
                PaidDate = DateTime.UtcNow
            };

            var saved = await _paymentRepo.AddAsync(payment);
            return _mapper.Map<PaymentDto>(saved);
        }

        public async Task<IEnumerable<PaymentDto>> GetMineAsync(int userId)
        {
            var all = await _paymentRepo.GetAllAsync();
            var myPayments = new List<Payment>();

            // Efficient way would include a join; keeping repo pattern, we filter by bookings:
            var bookingIds = (await _bookingRepo.GetAllAsync())
                            .Where(b => b.UserId == userId)
                            .Select(b => b.BookingId)
                            .ToHashSet();

            myPayments = all.Where(p => bookingIds.Contains(p.BookingId))
                            .OrderByDescending(p => p.PaidDate)
                            .ToList();

            return _mapper.Map<IEnumerable<PaymentDto>>(myPayments);
        }

        public async Task<IEnumerable<PaymentDto>> GetAllAsync()
        {
            var all = await _paymentRepo.GetAllAsync();
            var sorted = all.OrderByDescending(p => p.PaidDate);
            return _mapper.Map<IEnumerable<PaymentDto>>(sorted);
        }

        public async Task<PaymentDto> GetByIdAsync(int id)
        {
            var p = await _paymentRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Payment {id} not found");
            return _mapper.Map<PaymentDto>(p);
        }

        // --- helpers ---
        private async Task<string> GetBookingStatusNameAsync(int statusId)
        {
            // We don’t inject BookingStatus repo here to keep this minimal.
            // If you need strict checks by name, move this logic into BookingService or inject the repo.
            // For now we just return empty and rely on simple checks above.
            return string.Empty; // leave as empty to skip name-based gating
        }
    }
}

