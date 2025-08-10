using System.Threading.Tasks;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Interfaces
{
    public interface IPasswordService
    {
        Task RequestPasswordResetAsync(ForgotPasswordRequestDto dto);
        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
    }
}
