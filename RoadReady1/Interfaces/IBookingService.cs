using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IBookingService
    {
        Task<BookingQuoteDto> QuoteAsync(BookingQuoteRequestDto req);
        Task<BookingDto> CreateAsync(int userId, BookingCreateDto dto);
        Task<IEnumerable<BookingDto>> GetMineAsync(int userId);

        // staff
        Task<IEnumerable<BookingDto>> GetAllAsync();
        Task<BookingDto> GetByIdAsync(int id);

        // customer
        Task CancelAsync(int userId, int bookingId);
    }
}
