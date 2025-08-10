using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IBookingIssueService
    {
        Task<BookingIssueDto> CreateAsync(int userId, BookingIssueCreateDto dto);
        Task<IEnumerable<BookingIssueDto>> GetMineAsync(int userId);
        Task<IEnumerable<BookingIssueDto>> GetAllAsync();               // staff
        Task<IEnumerable<BookingIssueDto>> GetByBookingAsync(int bookingId); // staff
        Task UpdateStatusAsync(int issueId, string status);             // staff
    }
}
