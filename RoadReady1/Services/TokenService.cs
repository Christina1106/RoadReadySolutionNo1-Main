using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RoadReady1.Interfaces;

namespace RoadReady1.Services
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public TokenService(IConfiguration config)
        {
            var section = config.GetSection("JwtSettings");
            var secret = section["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");

            _issuer = section["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer missing");
            _audience = section["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience missing");
            _expiryMinutes = int.Parse(section["ExpiryMinutes"] ?? "60"); // 👈 match appsettings

            // MUST match Program.cs, which uses Encoding.UTF8.GetBytes(secret)
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        // Keep your current signature; pass username = email, role = "Customer"/etc.
        public Task<string> GenerateTokenAsync(string username, string role)
        {
            var claims = new List<Claim>
            {


                new Claim(ClaimTypes.Email, username),
                // standard subject (what you showed in your decoded sample)
                new Claim(JwtRegisteredClaimNames.Sub, username),

                // a stable "uid" if you have one (optional):
                // new Claim("uid", userId.ToString()),

                // this must match Program.cs RoleClaimType = ClaimTypes.Role
                new Claim(ClaimTypes.Role, role),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }


    }
}
