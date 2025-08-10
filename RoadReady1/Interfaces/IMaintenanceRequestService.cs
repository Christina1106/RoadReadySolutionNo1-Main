// File: Interfaces/IMaintenanceRequestService.cs
using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IMaintenanceRequestService
    {
        Task<MaintenanceRequestDto> CreateAsync(int userId, string role, MaintenanceRequestCreateDto dto);
        Task ResolveAsync(int requestId);                                  // staff
        Task<IEnumerable<MaintenanceRequestDto>> GetOpenAsync();           // staff
        Task<IEnumerable<MaintenanceRequestDto>> GetByCarAsync(int carId); // staff
        Task<IEnumerable<MaintenanceRequestDto>> GetMineAsync(int userId); // reporter
        Task<MaintenanceRequestDto> GetByIdAsync(int id);                  // staff
    }
}
