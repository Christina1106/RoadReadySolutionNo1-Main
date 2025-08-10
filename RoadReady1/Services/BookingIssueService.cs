// File: Services/BookingIssueService.cs
using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class BookingIssueService : IBookingIssueService
    {
        private static readonly HashSet<string> _allowedStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Open", "In Progress", "Resolved", "Closed" };

        private readonly IRepository<int, BookingIssue> _issueRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IMapper _mapper;

        public BookingIssueService(
            IRepository<int, BookingIssue> issueRepo,
            IRepository<int, Booking> bookingRepo,
            IMapper mapper)
        {
            _issueRepo = issueRepo;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
        }

        public async Task<BookingIssueDto> CreateAsync(int userId, BookingIssueCreateDto dto)
        {
            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException($"Booking {dto.BookingId} not found");

            if (booking.UserId != userId)
                throw new UnauthorizedException("You can only report issues for your own bookings.");

            var entity = new BookingIssue
            {
                BookingId = dto.BookingId,
                UserId = userId,
                IssueType = dto.IssueType,
                Description = dto.Description,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            entity = await _issueRepo.AddAsync(entity);
            return _mapper.Map<BookingIssueDto>(entity);
        }

        public async Task<IEnumerable<BookingIssueDto>> GetMineAsync(int userId)
        {
            var all = await _issueRepo.GetAllAsync();
            var mine = all.Where(i => i.UserId == userId)
                          .OrderByDescending(i => i.CreatedAt);
            return _mapper.Map<IEnumerable<BookingIssueDto>>(mine);
        }

        public async Task<IEnumerable<BookingIssueDto>> GetAllAsync()
        {
            var all = await _issueRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<BookingIssueDto>>(all.OrderByDescending(i => i.CreatedAt));
        }

        public async Task<IEnumerable<BookingIssueDto>> GetByBookingAsync(int bookingId)
        {
            var all = await _issueRepo.GetAllAsync();
            var list = all.Where(i => i.BookingId == bookingId)
                          .OrderByDescending(i => i.CreatedAt);
            return _mapper.Map<IEnumerable<BookingIssueDto>>(list);
        }

        public async Task UpdateStatusAsync(int issueId, string status)
        {
            if (!_allowedStatuses.Contains(status))
                throw new BadRequestException("Invalid status. Allowed: Open, In Progress, Resolved, Closed.");

            var issue = await _issueRepo.GetByIdAsync(issueId)
                        ?? throw new NotFoundException($"Issue {issueId} not found");

            issue.Status = status;
            await _issueRepo.UpdateAsync(issueId, issue);
        }
    }
}
