using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using NUnit.Framework;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using RoadReady1.Services;

namespace RoadReadyTest
{
    public class PaymentServiceTests
    {
        private Mock<IRepository<int, Payment>> _paymentRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IRepository<int, PaymentMethod>> _methodRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private PaymentService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _paymentRepo = new Mock<IRepository<int, Payment>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _methodRepo = new Mock<IRepository<int, PaymentMethod>>();
            _mapper = new Mock<IMapper>();

            // Mapping Payment -> PaymentDto
            _mapper.Setup(m => m.Map<PaymentDto>(It.IsAny<Payment>()))
                   .Returns((Payment p) => new PaymentDto
                   {
                       PaymentId = p.PaymentId,
                       BookingId = p.BookingId,
                       MethodId = p.MethodId,
                       Amount = p.Amount,
                       PaymentStatus = p.PaymentStatus,
                       TransactionId = p.TransactionId,
                       PaidDate = p.PaidDate
                   });

            // Mapping IEnumerable<Payment> -> IEnumerable<PaymentDto>
            _mapper.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                   .Returns((IEnumerable<Payment> list) => list.Select(p => new PaymentDto
                   {
                       PaymentId = p.PaymentId,
                       BookingId = p.BookingId,
                       MethodId = p.MethodId,
                       Amount = p.Amount,
                       PaymentStatus = p.PaymentStatus,
                       TransactionId = p.TransactionId,
                       PaidDate = p.PaidDate
                   }).ToList());

            _svc = new PaymentService(_paymentRepo.Object, _bookingRepo.Object, _methodRepo.Object, _mapper.Object);
        }

        [Test]
        public void PayAsync_Throws_When_Booking_Not_Found()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Booking)null!);
            var dto = new PaymentCreateDto { BookingId = 1, MethodId = 1 };
            Assert.ThrowsAsync<NotFoundException>(() => _svc.PayAsync(10, "Customer", dto));
        }

        [Test]
        public void PayAsync_Throws_When_Not_Owner_And_Not_Staff()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking { BookingId = 5, UserId = 99, TotalAmount = 100m });
            _methodRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PaymentMethod { MethodId = 1, MethodName = "card" });
            var dto = new PaymentCreateDto { BookingId = 5, MethodId = 1 };
            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.PayAsync(10, "Customer", dto));
        }

        [Test]
        public void PayAsync_Throws_When_Method_Not_Found()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Booking { BookingId = 7, UserId = 10, TotalAmount = 120m });
            _methodRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((PaymentMethod)null!);
            var dto = new PaymentCreateDto { BookingId = 7, MethodId = 9 };
            Assert.ThrowsAsync<NotFoundException>(() => _svc.PayAsync(10, "Customer", dto));
        }

        [Test]
        public void PayAsync_Throws_On_Duplicate_Success_Payment()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Booking { BookingId = 7, UserId = 10, TotalAmount = 120m });
            _methodRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PaymentMethod { MethodId = 1, MethodName = "card" });

            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Payment { PaymentId=1, BookingId=7, PaymentStatus="Success", Amount=120m }
            });

            var dto = new PaymentCreateDto { BookingId = 7, MethodId = 1 };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.PayAsync(10, "Customer", dto));
        }

        [Test]
        public async Task PayAsync_Succeeds_For_Owner()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Booking { BookingId = 7, UserId = 10, TotalAmount = 120m });
            _methodRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PaymentMethod { MethodId = 1, MethodName = "card" });
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Payment>());
            _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                        .ReturnsAsync((Payment p) => { p.PaymentId = 555; return p; });

            var dto = new PaymentCreateDto { BookingId = 7, MethodId = 1 };
            var result = await _svc.PayAsync(10, "Customer", dto);

            Assert.That(result.PaymentId, Is.EqualTo(555));
            Assert.That(result.BookingId, Is.EqualTo(7));
            Assert.That(result.MethodId, Is.EqualTo(1));
            Assert.That(result.Amount, Is.EqualTo(120m));
            Assert.That(result.PaymentStatus.ToLower(), Is.EqualTo("success"));
        }

        [Test]
        public async Task GetMineAsync_Filters_By_User()
        {
            _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Payment { PaymentId=1, BookingId=10, Amount=50m, PaymentStatus="Success", PaidDate=DateTime.UtcNow.AddMinutes(-10) },
                new Payment { PaymentId=2, BookingId=20, Amount=60m, PaymentStatus="Success", PaidDate=DateTime.UtcNow.AddMinutes(-5) },
                new Payment { PaymentId=3, BookingId=30, Amount=70m, PaymentStatus="Success", PaidDate=DateTime.UtcNow.AddMinutes(-1) }
            });
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking { BookingId=10, UserId=7 },
                new Booking { BookingId=20, UserId=8 },
                new Booking { BookingId=30, UserId=7 }
            });

            var mine = (await _svc.GetMineAsync(7)).ToList();
            Assert.That(mine.Select(p => p.PaymentId), Is.EquivalentTo(new[] { 1, 3 }));
        }

        [Test]
        public async Task GetByIdAsync_Returns_Dto()
        {
            _paymentRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Payment
            {
                PaymentId = 11,
                BookingId = 7,
                MethodId = 1,
                Amount = 120m,
                PaymentStatus = "Success",
                PaidDate = DateTime.UtcNow
            });

            var dto = await _svc.GetByIdAsync(11);
            Assert.That(dto.PaymentId, Is.EqualTo(11));
            Assert.That(dto.Amount, Is.EqualTo(120m));
        }

        [Test]
        public void GetByIdAsync_Throws_NotFound()
        {
            _paymentRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Payment)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(404));
        }
    }
}
