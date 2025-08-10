using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IRefundService
    {
        Task<RefundDto> RequestAsync(int userId, RefundRequestCreateDto dto); // Customer
        Task<IEnumerable<RefundDto>> MineAsync(int userId);                    // Customer
        Task<IEnumerable<RefundDto>> GetAllAsync();                            // Staff
        Task<RefundDto> GetByIdAsync(int id);                                  // Staff
        Task ApproveAsync(int id);                                             // Staff
        Task RejectAsync(int id);                                              // Staff
    }
}
