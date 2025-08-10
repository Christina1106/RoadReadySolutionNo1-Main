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
    public class RefundServiceTests
    {
        private Mock<IRepository<int, Refund>> _refundRepo = null!;
        private Mock<IRepository<int, Payment>> _paymentRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private RefundService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _refundRepo = new Mock<IRepository<int, Refund>>();
            _paymentRepo = new Mock<IRepository<int, Payment>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _mapper = new Mock<IMapper>();

            _mapper.Setup(m => m.Map<RefundDto>(It.IsAny<Refund>()))
                   .Returns((Refund r) => new RefundDto
                   {
                       RefundId = r.RefundId,
                       BookingId = r.BookingId,
                       PaymentId = r.PaymentId,
                       UserId = r.UserId,
                       Amount = r.Amount,
                       Reason = r.Reason,
                       Status = r.Status,
                       RequestedAt = r.RequestedAt,
                       ProcessedAt = r.ProcessedAt
                   });

            _mapper.Setup(m => m.Map<IEnumerable<RefundDto>>(It.IsAny<IEnumerable<Refund>>()))
                   .Returns((IEnumerable<Refund> list) => list.Select(r => new RefundDto
                   {
                       RefundId = r.RefundId,
                       BookingId = r.BookingId,
                       PaymentId = r.PaymentId,
                       UserId = r.UserId,
                       Amount = r.Amount,
                       Reason = r.Reason,
                       Status = r.Status,
                       RequestedAt = r.RequestedAt,
                       ProcessedAt = r.ProcessedAt
                   }).ToList());

            _svc = new RefundService(_refundRepo.Object, _paymentRepo.Object, _bookingRepo.Object, _mapper.Object);
        }

        // ---- Request ----

        [Test]
        public void RequestAsync_Throws_When_Amount_Invalid()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 0 };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Booking_Not_Found()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Booking)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Not_Owner()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 99 });
            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Payment_Not_Found()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Payment)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Payment_Not_For_Booking()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Payment { PaymentId = 2, BookingId = 99, Amount = 50m, PaymentStatus = "Success" });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Payment_Not_Success()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };
            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Payment { PaymentId = 2, BookingId = 1, Amount = 50m, PaymentStatus = "Failed" });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Pending_Or_Refunded_Exists()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 10m };

            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Payment { PaymentId = 2, BookingId = 1, Amount = 50m, PaymentStatus = "Success" });
            _refundRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>()))
                       .ReturnsAsync(new Refund { RefundId = 9, Status = "Pending" });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public void RequestAsync_Throws_When_Amount_Exceeds_Payment()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 60m };

            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Payment { PaymentId = 2, BookingId = 1, Amount = 50m, PaymentStatus = "Success" });
            _refundRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>()))
                       .ReturnsAsync((Refund)null!);

            Assert.ThrowsAsync<BadRequestException>(() => _svc.RequestAsync(10, dto));
        }

        [Test]
        public async Task RequestAsync_Succeeds()
        {
            var dto = new RefundRequestCreateDto { BookingId = 1, PaymentId = 2, Amount = 30m, Reason = "Change of plans" };

            _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 10 });
            _paymentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Payment { PaymentId = 2, BookingId = 1, Amount = 50m, PaymentStatus = "Success" });
            _refundRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>()))
                       .ReturnsAsync((Refund)null!);

            _refundRepo.Setup(r => r.AddAsync(It.IsAny<Refund>()))
                       .ReturnsAsync((Refund r) => { r.RefundId = 777; return r; });

            var result = await _svc.RequestAsync(10, dto);

            Assert.That(result.RefundId, Is.EqualTo(777));
            Assert.That(result.Amount, Is.EqualTo(30m));
            Assert.That(result.Status, Is.EqualTo("Pending"));
        }

        // ---- Mine / All / ById ----

        [Test]
        public async Task MineAsync_Filters_By_User()
        {
            _refundRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Refund { RefundId=1, UserId=10, RequestedAt=DateTime.UtcNow.AddMinutes(-1) },
                new Refund { RefundId=2, UserId=11, RequestedAt=DateTime.UtcNow.AddMinutes(-2) },
                new Refund { RefundId=3, UserId=10, RequestedAt=DateTime.UtcNow.AddMinutes(-3) }
            });

            var mine = (await _svc.MineAsync(10)).ToList();
            Assert.That(mine.Select(x => x.RefundId), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public async Task GetAllAsync_Returns_All_Sorted()
        {
            _refundRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Refund { RefundId=1, RequestedAt=DateTime.UtcNow.AddHours(-2) },
                new Refund { RefundId=2, RequestedAt=DateTime.UtcNow.AddHours(-1) }
            });

            var all = (await _svc.GetAllAsync()).ToList();
            Assert.That(all.Select(x => x.RefundId), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public async Task GetByIdAsync_Returns_Dto()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(new Refund { RefundId = 99, Amount = 10m, Status = "Pending" });
            var dto = await _svc.GetByIdAsync(99);
            Assert.That(dto.RefundId, Is.EqualTo(99));
        }

        [Test]
        public void GetByIdAsync_Throws_NotFound()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Refund)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(404));
        }

        // ---- Approve / Reject ----

        [Test]
        public void ApproveAsync_Throws_When_Not_Found()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Refund)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.ApproveAsync(1));
        }

        [Test]
        public void ApproveAsync_Throws_When_Not_Pending()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Refund { RefundId = 1, Status = "Rejected" });
            Assert.ThrowsAsync<BadRequestException>(() => _svc.ApproveAsync(1));
        }

        [Test]
        public void ApproveAsync_Throws_When_Payment_Not_Found()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Refund { RefundId = 1, Status = "Pending", PaymentId = 5 });
            _paymentRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Payment)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.ApproveAsync(1));
        }

        [Test]
        public async Task ApproveAsync_Sets_Refunded_And_Payment_Refunded()
        {
            var r = new Refund { RefundId = 1, Status = "Pending", PaymentId = 5 };
            _refundRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(r);
            _paymentRepo.Setup(repo => repo.GetByIdAsync(5)).ReturnsAsync(new Payment { PaymentId = 5, PaymentStatus = "Success" });

            _refundRepo.Setup(repo => repo.UpdateAsync(1, It.IsAny<Refund>()))
                       .ReturnsAsync((int _, Refund updated) => updated);
            _paymentRepo.Setup(repo => repo.UpdateAsync(5, It.IsAny<Payment>()))
                        .ReturnsAsync((int _, Payment updated) => updated);

            await _svc.ApproveAsync(1);

            _refundRepo.Verify(repo => repo.UpdateAsync(1, It.Is<Refund>(x => x.Status == "Refunded" && x.ProcessedAt != null)), Times.Once);
            _paymentRepo.Verify(repo => repo.UpdateAsync(5, It.Is<Payment>(x => x.PaymentStatus == "Refunded")), Times.Once);
        }

        [Test]
        public void RejectAsync_Throws_When_Not_Found()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Refund)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.RejectAsync(2));
        }

        [Test]
        public void RejectAsync_Throws_When_Not_Pending()
        {
            _refundRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Refund { RefundId = 2, Status = "Refunded" });
            Assert.ThrowsAsync<BadRequestException>(() => _svc.RejectAsync(2));
        }

        [Test]
        public async Task RejectAsync_Sets_Rejected()
        {
            var r = new Refund { RefundId = 2, Status = "Pending" };
            _refundRepo.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(r);
            _refundRepo.Setup(repo => repo.UpdateAsync(2, It.IsAny<Refund>()))
                       .ReturnsAsync((int _, Refund updated) => updated);

            await _svc.RejectAsync(2);

            _refundRepo.Verify(repo => repo.UpdateAsync(2, It.Is<Refund>(x => x.Status == "Rejected" && x.ProcessedAt != null)), Times.Once);
        }
    }
}
