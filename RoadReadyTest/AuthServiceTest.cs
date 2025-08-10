using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using RoadReady1.Services;

namespace RoadReadyTest
{
    public class AuthServiceTest
    {
        private Mock<IRepository<int, User>> _userRepo = null!;
        private Mock<IRepository<int, Role>> _roleRepo = null!;
        private Mock<IPasswordHasher<User>> _hasher = null!;
        private Mock<IMapper> _mapper = null!;
        private IConfiguration _config = null!;

        [SetUp]
        public void Setup()
        {
            _userRepo = new Mock<IRepository<int, User>>();
            _roleRepo = new Mock<IRepository<int, Role>>();
            _hasher = new Mock<IPasswordHasher<User>>();
            _mapper = new Mock<IMapper>();

            var kv = new[]
            {
                new KeyValuePair<string,string?>("JwtSettings:SecretKey", "sB9Q4vG6hXy7ZJ2uWq8LpAa1KdRfUvWxYz01GhIjKlMn="),
                new KeyValuePair<string,string?>("JwtSettings:Issuer", "RoadReadyAPI"),
                new KeyValuePair<string,string?>("JwtSettings:Audience", "RoadReadyUsers"),
                new KeyValuePair<string,string?>("JwtSettings:ExpiryMinutes", "5"),
            };
            _config = new ConfigurationBuilder().AddInMemoryCollection(kv).Build();
        }

        [Test]
        public async Task LoginAsync_Returns_JWT_String()
        {
            var dto = new UserLoginDto { Email = "admin@example.com", Password = "pass" };
            var user = new User { UserId = 1, Email = dto.Email, PasswordHash = "HASH", RoleId = 1 };
            _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
                     .ReturnsAsync(user);

            _hasher.Setup(h => h.VerifyHashedPassword(user, "HASH", "pass"))
                   .Returns(PasswordVerificationResult.Success);

            _roleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Role { RoleId = 1, RoleName = "Admin" });

            var svc = new AuthService(_userRepo.Object, _roleRepo.Object, _hasher.Object, _config, _mapper.Object);

            var token = await svc.LoginAsync(dto);

            Assert.That(token, Is.Not.Null.And.Not.Empty);
            Assert.That(token.Split('.').Length, Is.EqualTo(3)); // header.payload.signature
        }
    }
}





//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using NUnit.Framework;
//using RoadReady1.Context;
//using RoadReady1.Interfaces;
//using RoadReady1.Models;
//using RoadReady1.Models.DTOs;
//using RoadReady1.Services;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace RoadReadyTest
//{
//    [TestFixture]
//    public class AuthServiceTest
//    {
//        private RoadReadyDbContext _context;
//        private IAuthService _authService;

//        [SetUp]
//        public void Setup()
//        {
//            var options = new DbContextOptionsBuilder<RoadReadyDbContext>()
//                .UseInMemoryDatabase(databaseName: "TestDb")
//                .Options;

//            _context = new RoadReadyDbContext(options);

//            _context.Roles.Add(new Role { RoleId = 1, RoleName = "Tester" });
//            _context.SaveChanges();

//            var config = new ConfigurationBuilder()
//                .AddInMemoryCollection(new Dictionary<string, string>
//                {
//                    {"Jwt:Key", "ThisIsASecretTestKeyForJwtTokenGeneration"},
//                    {"Jwt:Issuer", "RoadReadyTestIssuer"},
//                    {"Jwt:Audience", "RoadReadyTestAudience"}
//                })
//                .Build();

//            _authService = new AuthService(_context, config);
//        }

//        [Test]
//        public async Task Register_NewUser_ReturnsToken()
//        {
//            var dto = new UserRegisterDTO
//            {
//                FirstName = "Unit",
//                LastName = "Tester",
//                Email = "unit@test.com",
//                PhoneNumber = "1234567890",
//                Password = "Test123",
//                RoleId = 1
//            };

//            var result = await _authService.Register(dto);

//            Assert.IsNotNull(result);
//            Assert.AreEqual(dto.Email, result.Email);
//            Assert.IsNotNull(result.Token);
//        }

//        [Test]
//        public async Task Login_ValidUser_ReturnsToken()
//        {
//            // register first
//            var registerDto = new UserRegisterDTO
//            {
//                FirstName = "Login",
//                LastName = "User",
//                Email = "login@test.com",
//                PhoneNumber = "9876543210",
//                Password = "LoginPass",
//                RoleId = 1
//            };
//            await _authService.Register(registerDto);

//            // login
//            var loginDto = new UserLoginDTO
//            {
//                Email = "login@test.com",
//                Password = "LoginPass"
//            };

//            var tokenUser = await _authService.Login(loginDto);

//            Assert.IsNotNull(tokenUser);
//            Assert.AreEqual(loginDto.Email, tokenUser.Email);
//            Assert.IsNotNull(tokenUser.Token);
//        }

//        [Test]
//        public void Register_DuplicateEmail_ThrowsException()
//        {
//            var dto = new UserRegisterDTO
//            {
//                FirstName = "Dup",
//                LastName = "User",
//                Email = "dup@test.com",
//                PhoneNumber = "1112223333",
//                Password = "pass",
//                RoleId = 1
//            };

//            _authService.Register(dto).Wait();

//            var ex = Assert.ThrowsAsync<System.Exception>(async () =>
//            {
//                await _authService.Register(dto);
//            });

//            Assert.That(ex.Message, Is.EqualTo("User already exists"));
//        }

//        [Test]
//        public void Login_InvalidCredentials_ThrowsException()
//        {
//            var loginDto = new UserLoginDTO
//            {
//                Email = "invalid@test.com",
//                Password = "wrongpass"
//            };

//            var ex = Assert.ThrowsAsync<System.Exception>(async () =>
//            {
//                await _authService.Login(loginDto);
//            });

//            Assert.That(ex.Message, Is.EqualTo("Invalid credentials"));
//        }
//        [Test]
//        public async Task Register_ShouldGenerateToken()
//        {

//            var dto = new UserRegisterDTO
//            {
//                FirstName = "Token",
//                LastName = "Check",
//                Email = "tokencheck@test.com",
//                PhoneNumber = "9999999999",
//                Password = "secure",
//                RoleId = 1
//            };

//            // Act
//            var result = await _authService.Register(dto);


//            Assert.IsNotNull(result.Token, "Token should not be null");
//            Assert.IsNotEmpty(result.Token, "Token should not be empty");
//            TestContext.WriteLine("Generated Token: " + result.Token); // to view the token
//        }

//        [Test]
//        public async Task Login_ShouldReturnToken()
//        {
//            // First register a user
//            var registerDto = new UserRegisterDTO
//            {
//                FirstName = "Login",
//                LastName = "User",
//                Email = "logintoken@test.com",
//                PhoneNumber = "8888888888",
//                Password = "testpass",
//                RoleId = 1
//            };
//            await _authService.Register(registerDto);


//            var loginDto = new UserLoginDTO
//            {
//                Email = "logintoken@test.com",
//                Password = "testpass"
//            };

//            var result = await _authService.Login(loginDto);

//            Assert.IsNotNull(result.Token, "Token should not be null after login");
//            Assert.IsNotEmpty(result.Token, "Token should not be empty after login");
//            TestContext.WriteLine("JWT Token: " + result.Token); 
//        }


//        [TearDown]
//        public void TearDown()
//        {
//            _context.Database.EnsureDeleted();
//            _context.Dispose();
//        }
//    }
//}

