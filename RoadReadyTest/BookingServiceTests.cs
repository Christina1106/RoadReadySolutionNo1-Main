// File: RoadReadyTest/BookingServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class BookingServiceTests
    {
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IRepository<int, Car>> _carRepo = null!;
        private Mock<IRepository<int, BookingStatus>> _statusRepo = null!;
        private Mock<IRepository<int, Location>> _locationRepo = null!;
        private Mock<ICarService> _carService = null!;
        private Mock<IMapper> _mapper = null!;
        private BookingService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _carRepo = new Mock<IRepository<int, Car>>();
            _statusRepo = new Mock<IRepository<int, BookingStatus>>();
            _locationRepo = new Mock<IRepository<int, Location>>();
            _carService = new Mock<ICarService>();
            _mapper = new Mock<IMapper>();

            // Minimal map: Booking -> BookingDto (service enriches names & times after this)
            _mapper.Setup(m => m.Map<BookingDto>(It.IsAny<Booking>()))
                   .Returns((Booking b) => new BookingDto
                   {
                       BookingId = b.BookingId,
                       UserId = b.UserId,
                       CarId = b.CarId,
                       PickupLocationId = b.PickupLocationId,
                       DropoffLocationId = b.DropoffLocationId,
                       StatusId = b.StatusId,
                       TotalAmount = b.TotalAmount
                   });

            // ✅ BookingService with 6 constructor args (booking-only version)
            _svc = new BookingService(
                _bookingRepo.Object,
                _carRepo.Object,
                _statusRepo.Object,
                _locationRepo.Object,
                _carService.Object,
                _mapper.Object
            );
        }

        // ---------- Quote ----------

        [Test]
        public void QuoteAsync_Throws_On_Invalid_Date_Range()
        {
            var req = new BookingQuoteRequestDto
            {
                CarId = 1,
                FromUtc = DateTime.UtcNow.AddDays(2),
                ToUtc = DateTime.UtcNow.AddDays(1)
            };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.QuoteAsync(req));
        }

        [Test]
        public async Task QuoteAsync_Computes_Total_Correctly()
        {
            var from = new DateTime(2025, 8, 12, 9, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 8, 14, 9, 0, 0, DateTimeKind.Utc); // 2 days

            _carRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Car { CarId = 1, DailyRate = 100m });

            var q = await _svc.QuoteAsync(new BookingQuoteRequestDto { CarId = 1, FromUtc = from, ToUtc = to });

            Assert.That(q.Days, Is.EqualTo(2));
            Assert.That(q.DailyRate, Is.EqualTo(100m));
            Assert.That(q.Subtotal, Is.EqualTo(200m));
            Assert.That(q.Taxes, Is.EqualTo(Math.Round(200m * 0.12m, 2)));
            Assert.That(q.Total, Is.EqualTo(200m + Math.Round(200m * 0.12m, 2)));
        }

        // ---------- Create ----------

        [Test]
        public void CreateAsync_Throws_On_Dropoff_Before_Pickup()
        {
            var dto = new BookingCreateDto
            {
                CarId = 1,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = DateTime.UtcNow.AddDays(2),
                DropoffDateTimeUtc = DateTime.UtcNow.AddDays(1)
            };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(99, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Car_Not_Found()
        {
            var dto = new BookingCreateDto
            {
                CarId = 999,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = DateTime.UtcNow.AddDays(1),
                DropoffDateTimeUtc = DateTime.UtcNow.AddDays(2)
            };

            _carRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Car)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(42, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Location_Not_Found()
        {
            var dto = new BookingCreateDto
            {
                CarId = 1,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = DateTime.UtcNow.AddDays(1),
                DropoffDateTimeUtc = DateTime.UtcNow.AddDays(2)
            };

            _carRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Car { CarId = 1, DailyRate = 50m });
            _locationRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Location)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(42, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Pending_Status_Missing()
        {
            var dto = new BookingCreateDto
            {
                CarId = 1,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = DateTime.UtcNow.AddDays(1),
                DropoffDateTimeUtc = DateTime.UtcNow.AddDays(2)
            };

            _carRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Car { CarId = 1, DailyRate = 50m });
            _locationRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Location { LocationId = 10, LocationName = "A" });
            _locationRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Location { LocationId = 20, LocationName = "B" });

            _carService.Setup(s => s.EnsureAvailableAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                       .Returns(Task.CompletedTask);

            // No "pending" status in table
            _statusRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BookingStatus, bool>>>()))
                       .ReturnsAsync((BookingStatus)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(55, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Car_Not_Available()
        {
            var dto = new BookingCreateDto
            {
                CarId = 1,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = DateTime.UtcNow.AddDays(1),
                DropoffDateTimeUtc = DateTime.UtcNow.AddDays(2)
            };

            _carRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Car { CarId = 1, DailyRate = 50m });
            _locationRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Location { LocationId = 10, LocationName = "A" });
            _locationRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Location { LocationId = 20, LocationName = "B" });

            _carService.Setup(s => s.EnsureAvailableAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                       .ThrowsAsync(new BadRequestException("Not available"));

            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(55, dto));
        }

        [Test]
        public async Task CreateAsync_Succeeds_And_Returns_Dto()
        {
            var userId = 77;
            var from = DateTime.UtcNow.AddDays(1);
            var to = from.AddDays(2);

            var dto = new BookingCreateDto
            {
                CarId = 1,
                PickupLocationId = 10,
                DropoffLocationId = 20,
                PickupDateTimeUtc = from,
                DropoffDateTimeUtc = to
            };

            _carRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Car { CarId = 1, ModelName = "Civic", DailyRate = 100m });
            _locationRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Location { LocationId = 10, LocationName = "A" });
            _locationRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Location { LocationId = 20, LocationName = "B" });

            _carService.Setup(s => s.EnsureAvailableAsync(1, from, to))
                       .Returns(Task.CompletedTask);

            _statusRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BookingStatus, bool>>>()))
                       .ReturnsAsync(new BookingStatus { StatusId = 1, StatusName = "pending" });

            _bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                        .ReturnsAsync((Booking b) => { b.BookingId = 123; return b; });

            _statusRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new BookingStatus { StatusId = 1, StatusName = "pending" });

            var result = await _svc.CreateAsync(userId, dto);

            Assert.That(result.BookingId, Is.EqualTo(123));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.CarId, Is.EqualTo(1));
            Assert.That(result.CarName, Is.EqualTo("Civic")); // set in ToDto
            Assert.That(result.PickupLocationName, Is.EqualTo("A"));
            Assert.That(result.DropoffLocationName, Is.EqualTo("B"));
            Assert.That(result.StatusName, Is.EqualTo("pending"));
            Assert.That(result.PickupDateTimeUtc, Is.EqualTo(from));
            Assert.That(result.DropoffDateTimeUtc, Is.EqualTo(to));
        }

        // ---------- Cancel ----------

        [Test]
        public void CancelAsync_Throws_When_Booking_Not_Found()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Booking)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.CancelAsync(7, 999));
        }

        [Test]
        public void CancelAsync_Throws_When_User_Not_Owner()
        {
            var b = new Booking
            {
                BookingId = 22,
                UserId = 7, // owner is 7
                PickupDateTime = DateTime.UtcNow.AddDays(1),
                StatusId = 1
            };
            _bookingRepo.Setup(r => r.GetByIdAsync(22)).ReturnsAsync(b);

            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.CancelAsync(8, 22)); // caller is 8
        }

        [Test]
        public void CancelAsync_Throws_When_OnOrAfter_Pickup()
        {
            var b = new Booking
            {
                BookingId = 23,
                UserId = 7,
                PickupDateTime = DateTime.UtcNow, // now
                StatusId = 1
            };
            _bookingRepo.Setup(r => r.GetByIdAsync(23)).ReturnsAsync(b);

            Assert.ThrowsAsync<BadRequestException>(() => _svc.CancelAsync(7, 23));
        }

        [Test]
        public async Task CancelAsync_Sets_Status_To_Cancelled()
        {
            var b = new Booking
            {
                BookingId = 24,
                UserId = 7,
                PickupDateTime = DateTime.UtcNow.AddDays(2),
                StatusId = 1
            };
            _bookingRepo.Setup(r => r.GetByIdAsync(24)).ReturnsAsync(b);

            _statusRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BookingStatus, bool>>>()))
                       .ReturnsAsync(new BookingStatus { StatusId = 3, StatusName = "cancelled" });

            _bookingRepo.Setup(r => r.UpdateAsync(24, It.IsAny<Booking>()))
                        .ReturnsAsync((int _, Booking updated) => updated);

            await _svc.CancelAsync(7, 24);

            _bookingRepo.Verify(r => r.UpdateAsync(24, It.Is<Booking>(x => x.StatusId == 3)), Times.Once);
        }

        // ---------- GetMine / GetAll / GetById ----------

        [Test]
        public async Task GetMineAsync_Returns_Only_User_Bookings()
        {
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking { BookingId = 1, UserId = 10, PickupLocationId=1, DropoffLocationId=2, CarId=1, StatusId=1 },
                new Booking { BookingId = 2, UserId = 11, PickupLocationId=1, DropoffLocationId=2, CarId=1, StatusId=1 },
                new Booking { BookingId = 3, UserId = 10, PickupLocationId=1, DropoffLocationId=2, CarId=2, StatusId=1 }
            });

            // Minimal enrichment dependencies:
            _carRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Car { CarId = 1, ModelName = "X", DailyRate = 10m });
            _locationRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Location { LocationId = 1, LocationName = "L" });
            _statusRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new BookingStatus { StatusId = 1, StatusName = "pending" });

            var mine = (await _svc.GetMineAsync(10)).ToList();
            Assert.That(mine.Count, Is.EqualTo(2));
            Assert.That(mine.All(b => b.UserId == 10), Is.True);
        }

        [Test]
        public async Task GetAllAsync_Returns_All_Bookings()
        {
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking { BookingId = 1, UserId = 10, PickupLocationId=1, DropoffLocationId=2, CarId=1, StatusId=1 },
                new Booking { BookingId = 2, UserId = 11, PickupLocationId=1, DropoffLocationId=2, CarId=1, StatusId=1 },
            });

            _carRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Car { CarId = 1, ModelName = "X", DailyRate = 10m });
            _locationRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Location { LocationId = 1, LocationName = "L" });
            _statusRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new BookingStatus { StatusId = 1, StatusName = "pending" });

            var all = (await _svc.GetAllAsync()).ToList();
            Assert.That(all.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetByIdAsync_Returns_Dto()
        {
            var b = new Booking
            {
                BookingId = 5,
                UserId = 10,
                CarId = 7,
                PickupLocationId = 1,
                DropoffLocationId = 2,
                StatusId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(1),
                DropoffDateTime = DateTime.UtcNow.AddDays(2)
            };

            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(b);
            _carRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Car { CarId = 7, ModelName = "Model" });
            _locationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Location { LocationId = 1, LocationName = "A" });
            _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Location { LocationId = 2, LocationName = "B" });
            _statusRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new BookingStatus { StatusId = 1, StatusName = "pending" });

            var dto = await _svc.GetByIdAsync(5);

            Assert.That(dto.BookingId, Is.EqualTo(5));
            Assert.That(dto.CarName, Is.EqualTo("Model"));
            Assert.That(dto.PickupLocationName, Is.EqualTo("A"));
            Assert.That(dto.DropoffLocationName, Is.EqualTo("B"));
            Assert.That(dto.StatusName, Is.EqualTo("pending"));
        }

        [Test]
        public void GetByIdAsync_Throws_NotFound()
        {
            _bookingRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Booking)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(404));
        }
    }
}
