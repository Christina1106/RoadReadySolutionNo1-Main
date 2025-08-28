using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;          // <-- add this
using RoadReady1.Context;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using RoadReady1.Services;
using RoadReady1.Exceptions;

namespace RoadReadyTest
{
    public class UserServiceTest
    {
        private Mock<IRepository<int, User>> _repo = null!;
        private Mock<IMapper> _mapper = null!;
        private Mock<IPasswordHasher<User>> _hasher = null!;
        private RoadReadyDbContext _db = null!;
        private UserService _svc = null!;

        [SetUp]
        public void Setup()
        {
            // 1) Mocks
            _repo = new Mock<IRepository<int, User>>();
            _mapper = new Mock<IMapper>();
            _hasher = new Mock<IPasswordHasher<User>>();

            // 2) InMemory DbContext for tests
            var options = new DbContextOptionsBuilder<RoadReadyDbContext>()
                .UseInMemoryDatabase(databaseName: "RoadReady_TestDB_" + Guid.NewGuid())
                .Options;
            _db = new RoadReadyDbContext(options);

            // 3) Service under test (match the constructor)
            _svc = new UserService(_repo.Object, _db, _mapper.Object, _hasher.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task CreateAsync_HashesPassword_And_Saves()
        {
            var create = new UserCreateDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                PhoneNumber = "9999999999",
                Password = "P@ssw0rd!",
                RoleId = 3
            };

            var entity = new User
            {
                FirstName = create.FirstName,
                LastName = create.LastName,
                Email = create.Email,
                PhoneNumber = create.PhoneNumber
            };

            _mapper.Setup(m => m.Map<User>(create)).Returns(entity);
            _hasher.Setup(h => h.HashPassword(entity, create.Password)).Returns("HASHED");
            _repo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 42; return u; });

            var mappedBack = new UserDto
            {
                UserId = 42,
                Email = create.Email,
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "9999999999"
            };
            _mapper.Setup(m => m.Map<UserDto>(It.Is<User>(u => u.UserId == 42))).Returns(mappedBack);

            var result = await _svc.CreateAsync(create);

            Assert.That(result.UserId, Is.EqualTo(42));
            Assert.That(result.Email, Is.EqualTo("jane@example.com"));
            _hasher.Verify(h => h.HashPassword(entity, "P@ssw0rd!"), Times.Once);
            _repo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "jane@example.com" && u.PasswordHash == "HASHED")), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_ReturnsDto()
        {
            var entity = new User { UserId = 7, Email = "a@b.com", FirstName = "A", LastName = "B", PhoneNumber = "123" };
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(entity);

            var dto = new UserDto { UserId = 7, Email = "a@b.com", FirstName = "A", LastName = "B", PhoneNumber = "123" };
            _mapper.Setup(m => m.Map<UserDto>(entity)).Returns(dto);

            var result = await _svc.GetByIdAsync(7);

            Assert.That(result.UserId, Is.EqualTo(7));
            Assert.That(result.Email, Is.EqualTo("a@b.com"));
        }

        [Test]
        public void GetByIdAsync_WhenMissing_ThrowsNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null!);
            Assert.ThrowsAsync<NotFoundException>(async () => await _svc.GetByIdAsync(99));
        }

        [Test]
        public async Task UpdateAsync_MapsAndSaves()
        {
            var existing = new User { UserId = 5, FirstName = "Old", LastName = "Name", Email = "old@x.com", PhoneNumber = "111" };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            var update = new UserUpdateDto { FirstName = "New", LastName = "Name", PhoneNumber = "222" };

            _mapper.Setup(m => m.Map(update, existing))
                   .Callback<UserUpdateDto, User>((src, dest) =>
                   {
                       dest.FirstName = src.FirstName;
                       dest.LastName = src.LastName;
                       dest.PhoneNumber = src.PhoneNumber;
                   });

            _repo.Setup(r => r.UpdateAsync(5, It.IsAny<User>())).ReturnsAsync((int _, User u) => u);

            var back = new UserDto { UserId = 5, FirstName = "New", LastName = "Name", Email = "old@x.com", PhoneNumber = "222" };
            _mapper.Setup(m => m.Map<UserDto>(It.Is<User>(u => u.UserId == 5 && u.FirstName == "New"))).Returns(back);

            var result = await _svc.UpdateAsync(5, update);

            Assert.That(result.FirstName, Is.EqualTo("New"));
            _repo.Verify(r => r.UpdateAsync(5, It.Is<User>(u => u.FirstName == "New" && u.PhoneNumber == "222")), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_CallsRepoDelete()
        {
            var existing = new User { UserId = 8 };
            _repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(existing);
            _repo.Setup(r => r.DeleteAsync(8)).ReturnsAsync(existing);

            await _svc.DeleteAsync(8);

            _repo.Verify(r => r.DeleteAsync(8), Times.Once);
        }
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
