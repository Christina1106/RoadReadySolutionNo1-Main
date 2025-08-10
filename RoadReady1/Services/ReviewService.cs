using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<int, Review> _reviewRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IMapper _mapper;

        public ReviewService(
            IRepository<int, Review> reviewRepo,
            IRepository<int, Booking> bookingRepo,
            IMapper mapper)
        {
            _reviewRepo = reviewRepo;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
        }

        public async Task<ReviewDto> CreateAsync(int userId, ReviewCreateDto dto)
        {
            ValidateRating(dto.Rating);

            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException($"Booking {dto.BookingId} not found");

            if (booking.UserId != userId)
                throw new UnauthorizedException("You can only review your own bookings.");

            // Prevent reviewing before the trip ends (optional but sensible)
            if (booking.DropoffDateTime > DateTime.UtcNow)
                throw new BadRequestException("You can review only after the dropoff time.");

            // One review per booking per user
            var existing = await _reviewRepo.FindAsync(r => r.BookingId == dto.BookingId && r.UserId == userId);
            if (existing != null)
                throw new BadRequestException("You have already reviewed this booking.");

            var entity = new Review
            {
                BookingId = booking.BookingId,
                UserId = userId,
                CarId = booking.CarId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedDate = DateTime.UtcNow
            };

            entity = await _reviewRepo.AddAsync(entity);
            return _mapper.Map<ReviewDto>(entity);
        }

        public async Task<ReviewDto> UpdateAsync(int userId, int reviewId, ReviewUpdateDto dto)
        {
            ValidateRating(dto.Rating);

            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException($"Review {reviewId} not found");

            if (review.UserId != userId)
                throw new UnauthorizedException("You can only update your own review.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            var updated = await _reviewRepo.UpdateAsync(reviewId, review);
            return _mapper.Map<ReviewDto>(updated);
        }

        public async Task DeleteAsync(int userId, string role, int reviewId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException($"Review {reviewId} not found");

            var isOwner = review.UserId == userId;
            var isStaff = role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                       || role.Equals("RentalAgent", StringComparison.OrdinalIgnoreCase);

            if (!isOwner && !isStaff)
                throw new UnauthorizedException("You can only delete your own review.");

            await _reviewRepo.DeleteAsync(reviewId);
        }

        public async Task<IEnumerable<ReviewDto>> GetByCarAsync(int carId)
        {
            var all = await _reviewRepo.GetAllAsync();
            var list = all.Where(r => r.CarId == carId)
                          .OrderByDescending(r => r.CreatedDate);
            return _mapper.Map<IEnumerable<ReviewDto>>(list);
        }

        public async Task<IEnumerable<ReviewDto>> GetMineAsync(int userId)
        {
            var all = await _reviewRepo.GetAllAsync();
            var list = all.Where(r => r.UserId == userId)
                          .OrderByDescending(r => r.CreatedDate);
            return _mapper.Map<IEnumerable<ReviewDto>>(list);
        }

        public async Task<ReviewDto> GetByIdAsync(int id)
        {
            var r = await _reviewRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Review {id} not found");
            return _mapper.Map<ReviewDto>(r);
        }

        private static void ValidateRating(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new BadRequestException("Rating must be between 1 and 5.");
        }
    }
}
