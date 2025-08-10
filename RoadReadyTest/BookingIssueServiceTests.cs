// File: RoadReadyTest/BookingIssueServiceTests.cs
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
    public class BookingIssueServiceTests
    {
        private Mock<IRepository<int, BookingIssue>> _issueRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private BookingIssueService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _issueRepo = new Mock<IRepository<int, BookingIssue>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _mapper = new Mock<IMapper>();

            // Map BookingIssue -> BookingIssueDto
            _mapper.Setup(m => m.Map<BookingIssueDto>(It.IsAny<BookingIssue>()))
                   .Returns((BookingIssue e) => new BookingIssueDto
                   {
                       IssueId = e.IssueId,
                       BookingId = e.BookingId,
                       UserId = e.UserId,
                       IssueType = e.IssueType,
                       Description = e.Description,
                       Status = e.Status,
                       CreatedAt = e.CreatedAt
                   });

            _mapper.Setup(m => m.Map<IEnumerable<BookingIssueDto>>(It.IsAny<IEnumerable<BookingIssue>>()))
                   .Returns((IEnumerable<BookingIssue> list) => list.Select(e => new BookingIssueDto
                   {
                       IssueId = e.IssueId,
                       BookingId = e.BookingId,
                       UserId = e.UserId,
                       IssueType = e.IssueType,
                       Description = e.Description,
                       Status = e.Status,
                       CreatedAt = e.CreatedAt
                   }).ToList());

            _svc = new BookingIssueService(_issueRepo.Object, _bookingRepo.Object, _mapper.Object);
        }

        // ---------- Create ----------

        [Test]
        public void CreateAsync_Throws_When_Booking_Not_Found()
        {
            var dto = new BookingIssueCreateDto { BookingId = 99, IssueType = "Billing", Description = "Wrong charge." };
            _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_User_Not_Owner()
        {
            var dto = new BookingIssueCreateDto { BookingId = 5, IssueType = "Car Issue", Description = "AC not working." };
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking { BookingId = 5, UserId = 42 });

            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public async Task CreateAsync_Succeeds_Returns_Dto()
        {
            var now = DateTime.UtcNow;
            var dto = new BookingIssueCreateDto { BookingId = 7, IssueType = "Car Issue", Description = "Flat tire." };

            _bookingRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Booking { BookingId = 7, UserId = 10 });

            _issueRepo.Setup(r => r.AddAsync(It.IsAny<BookingIssue>()))
                      .ReturnsAsync((BookingIssue e) => { e.IssueId = 123; return e; });

            var result = await _svc.CreateAsync(10, dto);

            Assert.That(result.IssueId, Is.EqualTo(123));
            Assert.That(result.BookingId, Is.EqualTo(7));
            Assert.That(result.UserId, Is.EqualTo(10));
            Assert.That(result.IssueType, Is.EqualTo("Car Issue"));
            Assert.That(result.Description, Is.EqualTo("Flat tire."));
            Assert.That(result.Status, Is.EqualTo("Open"));
            Assert.That(result.CreatedAt, Is.Not.EqualTo(default(DateTime)));

            _issueRepo.Verify(r => r.AddAsync(It.Is<BookingIssue>(e =>
                e.BookingId == 7 &&
                e.UserId == 10 &&
                e.Status == "Open" &&
                e.Description == "Flat tire.")), Times.Once);
        }

        // ---------- GetMine ----------

        [Test]
        public async Task GetMineAsync_Returns_Only_Current_User_Issues()
        {
            var issues = new[]
            {
                new BookingIssue { IssueId=1, BookingId=11, UserId=10, Status="Open", CreatedAt=DateTime.UtcNow.AddHours(-1)},
                new BookingIssue { IssueId=2, BookingId=12, UserId=11, Status="Open", CreatedAt=DateTime.UtcNow.AddHours(-2)},
                new BookingIssue { IssueId=3, BookingId=13, UserId=10, Status="Resolved", CreatedAt=DateTime.UtcNow.AddHours(-3)},
            };
            _issueRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(issues);

            var mine = (await _svc.GetMineAsync(10)).ToList();

            Assert.That(mine.Count, Is.EqualTo(2));
            Assert.That(mine.All(i => i.UserId == 10), Is.True);
            // Sorted desc by CreatedAt -> IssueId 1 first
            Assert.That(mine.First().IssueId, Is.EqualTo(1));
        }

        // ---------- GetAll ----------

        [Test]
        public async Task GetAllAsync_Returns_All_Sorted_Desc()
        {
            var issues = new[]
            {
                new BookingIssue { IssueId=1, CreatedAt=DateTime.UtcNow.AddHours(-5)},
                new BookingIssue { IssueId=2, CreatedAt=DateTime.UtcNow.AddHours(-1)},
                new BookingIssue { IssueId=3, CreatedAt=DateTime.UtcNow.AddHours(-3)},
            };
            _issueRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(issues);

            var all = (await _svc.GetAllAsync()).ToList();

            Assert.That(all.Select(i => i.IssueId), Is.EqualTo(new[] { 2, 3, 1 }));
        }

        // ---------- GetByBooking ----------

        [Test]
        public async Task GetByBookingAsync_Filters_By_BookingId()
        {
            var issues = new[]
            {
                new BookingIssue { IssueId=1, BookingId=7, CreatedAt=DateTime.UtcNow.AddMinutes(-10)},
                new BookingIssue { IssueId=2, BookingId=8, CreatedAt=DateTime.UtcNow.AddMinutes(-5)},
                new BookingIssue { IssueId=3, BookingId=7, CreatedAt=DateTime.UtcNow.AddMinutes(-1)},
            };
            _issueRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(issues);

            var list = (await _svc.GetByBookingAsync(7)).ToList();

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.First().IssueId, Is.EqualTo(3)); // newest first
        }

        // ---------- UpdateStatus ----------

        [Test]
        public void UpdateStatusAsync_Throws_On_Invalid_Status()
        {
            Assert.ThrowsAsync<BadRequestException>(() => _svc.UpdateStatusAsync(5, "WeirdStatus"));
        }

        [Test]
        public void UpdateStatusAsync_Throws_When_Issue_Not_Found()
        {
            _issueRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BookingIssue)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.UpdateStatusAsync(99, "Open"));
        }

        [Test]
        public async Task UpdateStatusAsync_Succeeds()
        {
            var entity = new BookingIssue { IssueId = 55, Status = "Open" };
            _issueRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync(entity);

            _issueRepo.Setup(r => r.UpdateAsync(55, It.IsAny<BookingIssue>()))
                      .ReturnsAsync((int _, BookingIssue updated) => updated);

            await _svc.UpdateStatusAsync(55, "In Progress");

            _issueRepo.Verify(r => r.UpdateAsync(55, It.Is<BookingIssue>(i => i.Status == "In Progress")), Times.Once);
        }
    }
}
