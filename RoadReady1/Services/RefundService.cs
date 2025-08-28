using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class RefundService : IRefundService
    {
        private readonly IRepository<int, Refund> _refundRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IMapper _mapper;

        public RefundService(
            IRepository<int, Refund> refundRepo,
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Booking> bookingRepo,
            IMapper mapper)
        {
            _refundRepo = refundRepo;
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
        }

        // Customer: request a refund for own booking/payment
        public async Task<RefundDto> RequestAsync(int userId, RefundRequestCreateDto dto)
        {
            if (dto.Amount <= 0)
                throw new BadRequestException("Amount must be greater than 0.");

            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException($"Booking {dto.BookingId} not found");

            if (booking.UserId != userId)
                throw new UnauthorizedException("You can only request a refund for your own booking.");

            var payment = await _paymentRepo.GetByIdAsync(dto.PaymentId)
                          ?? throw new NotFoundException($"Payment {dto.PaymentId} not found");

            if (payment.BookingId != booking.BookingId)
                throw new BadRequestException("Payment does not belong to the given booking.");

            // Only allow refund for successful payments
            if (!string.Equals(payment.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only successful payments can be refunded.");

            // Prevent duplicate or already refunded
            var existing = await _refundRepo.FindAsync(r =>
                r.PaymentId == payment.PaymentId &&
                (r.Status == "Pending" || r.Status == "Refunded"));
            if (existing != null)
                throw new BadRequestException("There is already a pending or completed refund for this payment.");

            if (dto.Amount > payment.Amount)
                throw new BadRequestException("Refund amount cannot exceed the payment amount.");

            var entity = new Refund
            {
                BookingId = booking.BookingId,
                PaymentId = payment.PaymentId,
                UserId = userId,
                Amount = dto.Amount,
                Reason = dto.Reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            entity = await _refundRepo.AddAsync(entity);
            return _mapper.Map<RefundDto>(entity);
        }

        // Customer: my refund requests
        public async Task<IEnumerable<RefundDto>> MineAsync(int userId)
        {
            var all = await _refundRepo.GetAllAsync();
            var mine = all.Where(r => r.UserId == userId)
                          .OrderByDescending(r => r.RequestedAt);
            return _mapper.Map<IEnumerable<RefundDto>>(mine);
        }

        // Staff: list
        public async Task<IEnumerable<RefundDto>> GetAllAsync()
        {
            var all = await _refundRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<RefundDto>>(all.OrderByDescending(r => r.RequestedAt));
        }

        // Staff: detail
        public async Task<RefundDto> GetByIdAsync(int id)
        {
            var r = await _refundRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Refund {id} not found");
            return _mapper.Map<RefundDto>(r);
        }

        // Staff: approve -> mark refund + payment as refunded
        public async Task ApproveAsync(int id)
        {
            var r = await _refundRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Refund {id} not found");

            if (!string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only pending refunds can be approved.");

            var payment = await _paymentRepo.GetByIdAsync(r.PaymentId)
                          ?? throw new NotFoundException($"Payment {r.PaymentId} not found");

            // Flip statuses
            r.Status = "Refunded";
            r.ProcessedAt = DateTime.UtcNow;
            await _refundRepo.UpdateAsync(r.RefundId, r);

            payment.PaymentStatus = "Refunded";
            await _paymentRepo.UpdateAsync(payment.PaymentId, payment);
        }

        // Staff: reject
        public async Task RejectAsync(int id)
        {
            var r = await _refundRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Refund {id} not found");

            if (!string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only pending refunds can be rejected.");

            r.Status = "Rejected";
            r.ProcessedAt = DateTime.UtcNow;
            await _refundRepo.UpdateAsync(r.RefundId, r);
        }
    }
}
