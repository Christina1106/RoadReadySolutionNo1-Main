//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Configuration;
//using RoadReady1.Interfaces;
//using RoadReady1.Models;
//using RoadReady1.Models.DTOs;
//using RoadReady1.Exceptions;

//namespace RoadReady1.Services
//{
//    public class PasswordService : IPasswordService
//    {
//        private readonly IRepository<int, User> _userRepo;
//        private readonly IRepository<int, PasswordResetToken> _tokenRepo;
//        private readonly IPasswordHasher<User> _hasher;
//        private readonly IConfiguration _config;
//        private readonly IEmailService _emailService;

//        public PasswordService(
//            IRepository<int, User> userRepo,
//            IRepository<int, PasswordResetToken> tokenRepo,
//            IPasswordHasher<User> hasher,
//            IConfiguration config,
//            IEmailService emailService)
//        {
//            _userRepo = userRepo;
//            _tokenRepo = tokenRepo;
//            _hasher = hasher;
//            _config = config;
//            _emailService = emailService;
//        }

//        public async Task RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
//        {
//            // Do not reveal if the email does not exist
//            var user = await _userRepo.FindAsync(u => u.Email == dto.Email);
//            if (user == null) return;

//            // Generate token
//            var tokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
//            var expiryMins = int.Parse(_config["JwtSettings:PasswordResetExpiryMinutes"] ?? "60");
//            var expires = DateTime.UtcNow.AddMinutes(expiryMins);

//            var resetToken = new PasswordResetToken
//            {
//                UserId = user.UserId,
//                Token = tokenValue,
//                ExpiresAt = expires,
//                Used = false
//            };
//            await _tokenRepo.AddAsync(resetToken);

//            // Send email with link
//            var frontendUrl = _config["App:FrontendUrl"]?.TrimEnd('/')
//                              ?? throw new InvalidOperationException("App:FrontendUrl missing");
//            var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(tokenValue)}";
//            await _emailService.SendAsync(
//                user.Email,
//                "RoadReady Password Reset",
//                $"Click <a href=\"{resetLink}\">here</a> to reset your password. This link expires at {expires:O}."
//            );
//        }

//        public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
//        {
//            var record = await _tokenRepo.FindAsync(t => t.Token == dto.Token && !t.Used);
//            if (record == null || record.ExpiresAt < DateTime.UtcNow)
//                throw new InvalidTokenException();

//            var user = await _userRepo.GetByIdAsync(record.UserId);
//            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
//            await _userRepo.UpdateAsync(user.UserId, user);

//            record.Used = true;
//            await _tokenRepo.UpdateAsync(record.Id, record);
//        }
//    }
//}
