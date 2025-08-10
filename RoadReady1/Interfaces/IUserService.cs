using System.Collections.Generic;
using System.Threading.Tasks;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    /// <summary>
    /// Manages user CRUD operations and profile updates.
    /// </summary>
    public interface IUserService
    {
        Task<UserDto> CreateAsync(UserCreateDto dto);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(int userId);

        Task<UserDto> UpdateAsync(int userId, UserUpdateDto dto);
        Task DeleteAsync(int userId);
    }
}
