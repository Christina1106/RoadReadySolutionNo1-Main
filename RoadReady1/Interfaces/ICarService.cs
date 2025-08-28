using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface ICarService
    {
        Task<IEnumerable<CarDto>> GetAllAsync();
        Task<CarDto> GetByIdAsync(int id);
        Task<CarDto> CreateAsync(CarCreateDto dto);
        Task<CarDto> UpdateAsync(int id, CarUpdateDto dto);
        Task SetStatusAsync(int id, int statusId);
        Task DeleteAsync(int id);
        Task<IEnumerable<CarDto>> SearchAsync(CarSearchRequestDto req);

        Task<IEnumerable<CarDto>> SearchByBrandAsync(string brandName);

        // For BookingService later, and a simple availability endpoint now
        Task<bool> IsAvailableAsync(int carId, DateTime fromUtc, DateTime toUtc);
        Task EnsureAvailableAsync(int carId, DateTime fromUtc, DateTime toUtc); // throws CarUnavailableException
    }
}
