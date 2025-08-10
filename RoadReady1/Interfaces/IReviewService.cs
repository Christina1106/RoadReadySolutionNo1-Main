using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateAsync(int userId, ReviewCreateDto dto);               // Customer
        Task<ReviewDto> UpdateAsync(int userId, int reviewId, ReviewUpdateDto dto); // Owner
        Task DeleteAsync(int userId, string role, int reviewId);                    // Owner or Staff
        Task<IEnumerable<ReviewDto>> GetByCarAsync(int carId);                      // public
        Task<IEnumerable<ReviewDto>> GetMineAsync(int userId);                      // Customer
        Task<ReviewDto> GetByIdAsync(int id);                                       // Staff or for debugging
    }
}
