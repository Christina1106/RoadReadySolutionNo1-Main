// Services/UserService.cs
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadReady1.Context;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class UserService : IUserService
    {
        private readonly RoadReadyDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            RoadReadyDbContext db,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _db.Users.Include(u => u.Role).ToListAsync();
            return users.Select(ToDto);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id)
                ?? throw new NotFoundException($"User {id} not found");

            return ToDto(user);
        }

        public async Task<UserDto> CreateAsync(UserCreateDto dto)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == dto.RoleId)
                       ?? throw new NotFoundException($"Role {dto.RoleId} not found");

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName?.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber,
                RoleId = dto.RoleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            user.Role = role;

            return ToDto(user);
        }

        public async Task<UserDto> UpdateAsync(int id, UserUpdateDto dto)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id)
                ?? throw new NotFoundException($"User {id} not found");

            if (dto.FirstName is not null)  user.FirstName = dto.FirstName.Trim();
            if (dto.LastName  is not null)  user.LastName  = dto.LastName.Trim();
            if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber;

            if (dto.RoleId.HasValue && dto.RoleId.Value != user.RoleId)
            {
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == dto.RoleId.Value)
                           ?? throw new NotFoundException($"Role {dto.RoleId.Value} not found");
                user.RoleId = role.RoleId;
                user.Role = role;
            }

            await _db.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _db.Users.FindAsync(id)
                       ?? throw new NotFoundException($"User {id} not found");
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task ChangeRoleAsync(int userId, int? roleId, string? roleName)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException("User not found");

            Role role;
            if (roleId.HasValue)
            {
                role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId.Value)
                       ?? throw new NotFoundException("Role not found");
            }
            else
            {
                role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName)
                       ?? throw new NotFoundException("Role not found");
            }

            user.RoleId = role.RoleId;
            user.Role = role;
            await _db.SaveChangesAsync();
        }

        public async Task SetActiveAsync(int userId, bool isActive)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                       ?? throw new NotFoundException("User not found");

            user.IsActive = isActive;
            await _db.SaveChangesAsync();
        }

        private static UserDto ToDto(User u) => new UserDto
        {
            UserId = u.UserId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            RoleName = u.Role?.RoleName,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
