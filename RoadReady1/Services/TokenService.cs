using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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
        private readonly int _expiryDays;

        public TokenService(IConfiguration config)
        {
            var section = config.GetSection("JwtSettings");
            var secret = section["SecretKey"]
                         ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");
            _issuer = section["Issuer"];
            _audience = section["Audience"];
            _expiryDays = int.Parse(section["ExpiryDays"] ?? "1");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        public Task<string> GenerateTokenAsync(string username, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expiryDays),
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Task.FromResult(token);
        }
    }
}
