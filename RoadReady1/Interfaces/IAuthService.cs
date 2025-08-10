using RoadReady1.Models.DTOs;

public interface IAuthService
{
    Task RegisterAsync(UserRegisterDto dto);
    Task<string> LoginAsync(UserLoginDto dto);
}
