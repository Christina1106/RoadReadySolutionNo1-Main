using System.Threading.Tasks;

namespace RoadReady1.Interfaces
{
    /// <summary>
    /// Generates JWT tokens for a given username and role.
    /// </summary>
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(string username, string role);

    }
}
