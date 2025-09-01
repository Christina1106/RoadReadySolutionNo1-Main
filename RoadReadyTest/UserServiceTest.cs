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
        // kept only to satisfy old ctor calls (not used)
        private readonly IRepository<int, User>? _userRepo;

        private readonly RoadReadyDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;

        // New (preferred) constructor
        public UserService(
            RoadReadyDbContext db,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        // Back-compat overload to satisfy tests calling 4-arg ctor
        public UserService(
            IRepository<int, User> userRepo,
            RoadReadyDbContext db,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)
            : this(db, mapper, passwordHasher)
        {
            _userRepo = userRepo; // not used; just to keep old signature valid
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _db.Users.Include(u => u.Role).ToListAsync();
            return users.Select(ToDto);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.Include(u => u.Role)
                                  .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _db.Users.Include(u => u.Role)
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
            var user = await _db.Users.Include(u => u.Role)
                                      .FirstOrDefaultAsync(u => u.UserId == id)
                       ?? throw new NotFoundException($"User {id} not found");

            if (dto.FirstName is not null) user.FirstName = dto.FirstName.Trim();
            if (dto.LastName is not null) user.LastName = dto.LastName.Trim();
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
            var user = await _db.Users.Include(u => u.Role)
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



//using Microsoft.EntityFrameworkCore;
//using NUnit.Framework;
//using RoadReady1.Context;
//using RoadReady1.Exceptions;
//using RoadReady1.Models;
//using RoadReady1.Models.DTOs;
//using RoadReady1.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RoadReadyTest
//{
//    [TestFixture]
//    public class UserServiceTest
//    {
//        private RoadReadyDbContext _context;
//        private UserService _userService;

//        [SetUp]
//        public void Setup()
//        {
//            var options = new DbContextOptionsBuilder<RoadReadyDbContext>()
//                .UseInMemoryDatabase("UserServiceTestDb")
//                .Options;

//            _context = new RoadReadyDbContext(options);

//            _context.Roles.Add(new Role { RoleId = 1, RoleName = "Admin" });
//            _context.SaveChanges();

//            _userService = new UserService(_context);
//        }

//        [TearDown]
//        public void TearDown()
//        {
//            _context.Database.EnsureDeleted();
//            _context.Dispose();
//        }

//        [Test]
//        public void CreateUser_ShouldAddUser()
//        {
//            var dto = new UserDto
//            {
//                FirstName = "John",
//                LastName = "Doe",
//                Email = "john@example.com",
//                PhoneNumber = "1234567890",
//                Password = "password",
//                RoleId = 1
//            };

//            var result = _userService.CreateUser(dto);

//            Assert.IsNotNull(result);
//            Assert.AreEqual(dto.Email, result.Email);
//            Assert.IsTrue(result.UserId > 0);
//        }

//        [Test]
//        public void GetUserById_ValidId_ReturnsUser()
//        {
//            var user = new User
//            {
//                FirstName = "Jane",
//                LastName = "Smith",
//                Email = "jane@example.com",
//                PhoneNumber = "9999999999",
//                PasswordHash = "pwd",
//                RoleId = 1,
//                CreatedAt = DateTime.Now,
//                IsActive = true
//            };

//            _context.Users.Add(user);
//            _context.SaveChanges();

//            var result = _userService.GetUserById(user.UserId);

//            Assert.IsNotNull(result);
//            Assert.AreEqual(user.Email, result.Email);
//        }

//        [Test]
//        public void GetUserById_InvalidId_ThrowsNotFoundException()
//        {
//            var ex = Assert.Throws<NotFoundException>(() => _userService.GetUserById(999));
//            Assert.That(ex.Message, Does.Contain("not found"));
//        }

//        [Test]
//        public void UpdateUser_ValidId_UpdatesUser()
//        {
//            var user = _context.Users.Add(new User
//            {
//                FirstName = "Old",
//                LastName = "Name",
//                Email = "old@example.com",
//                PhoneNumber = "000",
//                PasswordHash = "123",
//                RoleId = 1,
//                CreatedAt = DateTime.Now,
//                IsActive = true
//            }).Entity;

//            _context.SaveChanges();

//            var updatedDto = new UserDto
//            {
//                FirstName = "New",
//                LastName = "Name",
//                Email = "new@example.com",
//                PhoneNumber = "111",
//                Password = "321",
//                RoleId = 1
//            };

//            _userService.UpdateUser(user.UserId, updatedDto);

//            var updatedUser = _context.Users.Find(user.UserId);
//            Assert.AreEqual("new@example.com", updatedUser.Email);
//            Assert.AreEqual("New", updatedUser.FirstName);
//        }

//        [Test]
//        public void UpdateUser_InvalidId_ThrowsNotFoundException()
//        {
//            var dto = new UserDto
//            {
//                FirstName = "Ghost",
//                LastName = "User",
//                Email = "ghost@example.com",
//                PhoneNumber = "000",
//                Password = "none",
//                RoleId = 1
//            };

//            var ex = Assert.Throws<NotFoundException>(() => _userService.UpdateUser(999, dto));
//            Assert.That(ex.Message, Does.Contain("not found"));
//        }

//        [Test]
//        public void DeleteUser_ValidId_RemovesUser()
//        {
//            var user = _context.Users.Add(new User
//            {
//                FirstName = "ToDelete",
//                LastName = "User",
//                Email = "delete@example.com",
//                PhoneNumber = "000",
//                PasswordHash = "abc",
//                RoleId = 1,
//                CreatedAt = DateTime.Now,
//                IsActive = true
//            }).Entity;
//            _context.SaveChanges();

//            _userService.DeleteUser(user.UserId);

//            var deletedUser = _context.Users.Find(user.UserId);
//            Assert.IsNull(deletedUser);
//        }

//        [Test]
//        public void DeleteUser_InvalidId_ThrowsNotFoundException()
//        {
//            int invalidId = 999;

//            var ex = Assert.Throws<NotFoundException>(() =>
//            {
//                _userService.DeleteUser(invalidId);
//            });

//            Assert.That(ex.Message, Is.EqualTo("User with ID 999 not found."));
//        }


//        [Test]
//        public void DummyReference()
//        {
//            var _ = typeof(UserService); // Forces reference
//        }

//    }
//}
