using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> PayAsync(int userId, string role, PaymentCreateDto dto);
        Task<IEnumerable<PaymentDto>> GetMineAsync(int userId);
        Task<IEnumerable<PaymentDto>> GetAllAsync();       // Admin/Agent
        Task<PaymentDto> GetByIdAsync(int id);             // Admin/Agent
    }
}
