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

        // Public register – HONORS RoleId (validated). If you want public=Customer only, set allowRoleOverride:false.
        public Task<UserDto> RegisterAsync(UserRegisterDto dto)
            => RegisterInternalAsync(dto, allowRoleOverride: true);

        // Admin register – honors RoleId
        public Task<UserDto> RegisterWithRoleAsync(UserRegisterDto dto)
            => RegisterInternalAsync(dto, allowRoleOverride: true);

        private async Task<UserDto> RegisterInternalAsync(UserRegisterDto dto, bool allowRoleOverride)
        {
            // 1) duplicates?
            var existing = await _userRepo.FindAsync(u => u.Email == dto.Email);
            if (existing != null) throw new UserAlreadyExistsException();

            // 2) decide & validate role
            int roleId = 3; // default to Customer
            if (allowRoleOverride)
            {
                if (dto.RoleId is 1 or 2 or 3)
                    roleId = dto.RoleId;
                else
                    throw new ArgumentException("Invalid RoleId. Allowed: 1=Admin, 2=RentalAgent, 3=Customer");
            }

            // 3) ensure role exists (and has the correct name mapping in DB)
            var role = await _roleRepo.GetByIdAsync(roleId)
                       ?? throw new ArgumentException($"RoleId {roleId} does not exist in Roles table");

            // 4) map & hash
            var user = _mapper.Map<User>(dto);
            user.RoleId = roleId;
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _userRepo.AddAsync(user);

            // 5) return safe DTO (include RoleId so controller can show it)
            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = role.RoleName,   // "Admin" / "RentalAgent" / "Customer"       // <-- helpful for Swagger verification
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
            var roleName = role.RoleName;

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
