// Services/UserService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            IRepository<int, User> userRepo,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)      // ← inject hasher
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new NotFoundException($"User {id} not found");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(UserCreateDto dto)
        {
            var userEntity = _mapper.Map<User>(dto);
            userEntity.PasswordHash = _passwordHasher.HashPassword(userEntity, dto.Password);
            userEntity.CreatedAt = DateTime.UtcNow;

            var created = await _userRepo.AddAsync(userEntity);
            return _mapper.Map<UserDto>(created);
        }

        public async Task<UserDto> UpdateAsync(int id, UserUpdateDto dto)
        {
            var userEntity = await _userRepo.GetByIdAsync(id);
            if (userEntity == null) throw new NotFoundException($"User {id} not found");

            _mapper.Map(dto, userEntity);
            var updated = await _userRepo.UpdateAsync(id, userEntity);
            return _mapper.Map<UserDto>(updated);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new NotFoundException($"User {id} not found");
            await _userRepo.DeleteAsync(id);
        }
    }
}
