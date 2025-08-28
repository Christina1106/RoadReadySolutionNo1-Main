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
        private readonly IRepository<int, User> _userRepo;
        private readonly RoadReadyDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            IRepository<int, User> userRepo,
            RoadReadyDbContext db,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepo = userRepo;
            _db = db;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _db.Users.Include(u => u.Role).ToListAsync();
            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                RoleName = u.Role.RoleName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) throw new NotFoundException($"User {id} not found");

            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> CreateAsync(UserCreateDto dto)
        {
            // (optional) ensure role exists
            var role = await _db.Roles.FindAsync(dto.RoleId);
            if (role == null) throw new NotFoundException($"Role {dto.RoleId} not found");

            var entity = _mapper.Map<User>(dto);
            entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;

            await _db.Users.AddAsync(entity);
            await _db.SaveChangesAsync();

            return new UserDto
            {
                UserId = entity.UserId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                PhoneNumber = entity.PhoneNumber,
                RoleName = role.RoleName,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<UserDto> UpdateAsync(int id, UserUpdateDto dto)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) throw new NotFoundException($"User {id} not found");

            _mapper.Map(dto, user);
            await _db.SaveChangesAsync();

            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) throw new NotFoundException($"User {id} not found");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
    }
}
