using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, Role> _roleRepo;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthService(
            IRepository<int, User> userRepo,
            IRepository<int, Role> roleRepo,
            IPasswordHasher<User> hasher,
            IConfiguration config,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _hasher = hasher;
            _config = config;
            _mapper = mapper;
        }

        // PUBLIC register -> always Customer (3)
        public async Task<UserDto> RegisterAsync(UserRegisterDto dto)
        {
            return await RegisterInternalAsync(dto, allowRoleOverride: false);
        }

        // ADMIN register -> honors RoleId (1/2/3)
        public async Task<UserDto> RegisterWithRoleAsync(UserRegisterDto dto)
        {
            return await RegisterInternalAsync(dto, allowRoleOverride: true);
        }

        private async Task<UserDto> RegisterInternalAsync(UserRegisterDto dto, bool allowRoleOverride)
        {
            // 1) duplicates?
            var existing = await _userRepo.FindAsync(u => u.Email == dto.Email);
            if (existing != null) throw new UserAlreadyExistsException();

            // 2) decide role
            int roleId = 3; // default Customer
            if (allowRoleOverride)
            {
                // only allow valid roles; fall back to Customer if out of range
                var requested = dto.RoleId;
                if (requested == 1 || requested == 2 || requested == 3)
                    roleId = requested;
            }

            // ensure role exists
            var role = await _roleRepo.GetByIdAsync(roleId);

            // 3) map + hash
            var user = _mapper.Map<User>(dto);
            user.RoleId = roleId;
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _userRepo.AddAsync(user);

            // 4) return safe DTO
            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = role?.RoleName ?? "Customer",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<string> LoginAsync(UserLoginDto dto)
        {
            var user = await _userRepo.FindAsync(u => u.Email == dto.Email);
            if (user == null) throw new UnauthorizedException();

            var verified = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verified == PasswordVerificationResult.Failed) throw new UnauthorizedException();

            var role = await _roleRepo.GetByIdAsync(user.RoleId);
            var roleName = role.RoleName; // "Admin" / "RentalAgent" / "Customer"

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("uid", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, roleName)
            };

            var secret = _config["JwtSettings:SecretKey"]!;
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var expiresMinutes = double.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "60");
            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
