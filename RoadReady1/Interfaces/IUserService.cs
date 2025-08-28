using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

        Task<User?> GetByEmailAsync(string email);

        Task<UserDto> UpdateAsync(int userId, UserUpdateDto dto);
        Task DeleteAsync(int userId);
    }
}
