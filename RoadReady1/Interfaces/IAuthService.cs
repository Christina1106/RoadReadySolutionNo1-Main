using System.Threading.Tasks;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    /// <summary>
    /// Authentication/authorization service:
    /// - Public registration
    /// - Admin registration (can set any role)
    /// - Login
    /// </summary>
    public interface IAuthService
    {
        // Public register: always Customer (3)
        Task<UserDto> RegisterAsync(UserRegisterDto dto);

        // Admin-only register: honors dto.RoleId (1/2/3)
        Task<UserDto> RegisterWithRoleAsync(UserRegisterDto dto);

        // Login returns JWT
        Task<string> LoginAsync(UserLoginDto dto);
    }
}
