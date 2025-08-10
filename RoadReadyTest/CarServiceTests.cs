using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using AutoMapper;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using RoadReady1.Services;
using RoadReady1.Exceptions;

namespace RoadReadyTest
{
    public class CarServiceTests
    {
        private Mock<IRepository<int, Car>> _carRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IRepository<int, CarBrand>> _brandRepo = null!;
        private Mock<IRepository<int, CarStatus>> _statusRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private CarService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _carRepo = new Mock<IRepository<int, Car>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _brandRepo = new Mock<IRepository<int, CarBrand>>();
            _statusRepo = new Mock<IRepository<int, CarStatus>>();
            _mapper = new Mock<IMapper>();

            // Default brand/status catalogs used by enrichment
            _brandRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CarBrand>
            {
                new CarBrand { BrandId = 1, BrandName = "Toyota" },
                new CarBrand { BrandId = 2, BrandName = "Honda" }
            });
            _statusRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CarStatus>
            {
                new CarStatus { StatusId = 1, StatusName = "Available" },
                new CarStatus { StatusId = 2, StatusName = "Reserved" }
            });

            // Simple mapper: CarCreateDto -> Car
            _mapper.Setup(m => m.Map<Car>(It.IsAny<CarCreateDto>()))
                .Returns((CarCreateDto d) => new Car
                {
                    BrandId = d.BrandId,
                    ModelName = d.ModelName,
                    Year = d.Year ?? 0,
                    FuelType = d.FuelType,
                    Transmission = d.Transmission,
                    Seats = d.Seats ?? 0,
                    DailyRate = d.DailyRate ?? 0m,
                    StatusId = d.StatusId,
                    ImageUrl = d.ImageUrl,
                    Description = d.Description
                });

            // Simple mapper: Car -> CarDto
            _mapper.Setup(m => m.Map<CarDto>(It.IsAny<Car>()))
                .Returns((Car c) => new CarDto
                {
                    CarId = c.CarId,
                    BrandId = c.BrandId,
                    ModelName = c.ModelName,
                    Year = c.Year,
                    FuelType = c.FuelType,
                    Transmission = c.Transmission,
                    Seats = c.Seats,
                    DailyRate = c.DailyRate,
                    StatusId = c.StatusId,
                    ImageUrl = c.ImageUrl,
                    Description = c.Description
                });

            // Map(update, entity)
            _mapper.Setup(m => m.Map(It.IsAny<CarUpdateDto>(), It.IsAny<Car>()))
                .Callback<CarUpdateDto, Car>((src, dest) =>
                {
                    dest.BrandId = src.BrandId;
                    dest.ModelName = src.ModelName;
                    dest.Year = src.Year ?? 0;
                    dest.FuelType = src.FuelType;
                    dest.Transmission = src.Transmission;
                    dest.Seats = src.Seats ?? 0;
                    dest.DailyRate = src.DailyRate ?? 0m;
                    dest.StatusId = src.StatusId;
                    dest.ImageUrl = src.ImageUrl;
                    dest.Description = src.Description;
                });

            _svc = new CarService(_carRepo.Object, _bookingRepo.Object, _brandRepo.Object, _statusRepo.Object, _mapper.Object);
        }

        [Test]
        public async Task GetAllAsync_Returns_Enriched_Dtos()
        {
            _carRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Car { CarId=10, BrandId=1, ModelName="Corolla", Year=2020, DailyRate=50m, StatusId=1 }
            });

            var cars = await _svc.GetAllAsync();

            var one = cars.Single();
            Assert.That(one.CarId, Is.EqualTo(10));
            Assert.That(one.BrandName, Is.EqualTo("Toyota")); // enriched from catalog
            Assert.That(one.StatusName, Is.EqualTo("Available"));
        }

        [Test]
        public async Task GetByIdAsync_WhenFound_Returns_Dto()
        {
            _carRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Car { CarId = 5, BrandId = 2, ModelName = "Civic", StatusId = 2 });
            var dto = await _svc.GetByIdAsync(5);
            Assert.That(dto.CarId, Is.EqualTo(5));
            Assert.That(dto.BrandName, Is.EqualTo("Honda"));
            Assert.That(dto.StatusName, Is.EqualTo("Reserved"));
        }

        [Test]
        public void GetByIdAsync_WhenMissing_Throws_NotFound()
        {
            _carRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Car)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(999));
        }

        [Test]
        public async Task CreateAsync_Succeeds_When_Valid_And_Not_Duplicate()
        {
            var dto = new CarCreateDto
            {
                BrandId = 1,
                ModelName = "Camry",
                Year = 2021,
                DailyRate = 80m,
                StatusId = 1
            };

            _brandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CarBrand { BrandId = 1, BrandName = "Toyota" });
            _statusRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CarStatus { StatusId = 1, StatusName = "Available" });
            _carRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Car, bool>>>())).ReturnsAsync((Car)null!);
            _carRepo.Setup(r => r.AddAsync(It.IsAny<Car>())).ReturnsAsync((Car c) => { c.CarId = 123; return c; });

            var result = await _svc.CreateAsync(dto);

            Assert.That(result.CarId, Is.EqualTo(123));
            Assert.That(result.BrandName, Is.EqualTo("Toyota"));
            Assert.That(result.StatusName, Is.EqualTo("Available"));
        }

        [Test]
        public void CreateAsync_Throws_BadRequest_On_Duplicate()
        {
            var dto = new CarCreateDto { BrandId = 1, ModelName = "Corolla", Year = 2020, StatusId = 1 };

            _brandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CarBrand { BrandId = 1, BrandName = "Toyota" });
            _statusRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CarStatus { StatusId = 1, StatusName = "Available" });
            _carRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Car, bool>>>()))
                    .ReturnsAsync(new Car { CarId = 77, BrandId = 1, ModelName = "Corolla", Year = 2020 });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(dto));
        }

        [Test]
        public async Task UpdateAsync_Saves_And_Returns_Dto()
        {
            _carRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Car { CarId = 9, BrandId = 1, ModelName = "Corolla", StatusId = 1 });
            _brandRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new CarBrand { BrandId = 2, BrandName = "Honda" });
            _statusRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new CarStatus { StatusId = 2, StatusName = "Reserved" });
            _carRepo.Setup(r => r.UpdateAsync(9, It.IsAny<Car>())).ReturnsAsync((int _, Car c) => c);

            var update = new CarUpdateDto { BrandId = 2, ModelName = "Civic", StatusId = 2, DailyRate = 70m, Seats = 5, Year = 2022 };

            var result = await _svc.UpdateAsync(9, update);

            Assert.That(result.ModelName, Is.EqualTo("Civic"));
            Assert.That(result.BrandName, Is.EqualTo("Honda"));
            Assert.That(result.StatusName, Is.EqualTo("Reserved"));
        }

        [Test]
        public void UpdateAsync_Throws_NotFound_For_Missing_Car()
        {
            _carRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Car)null!);
            var update = new CarUpdateDto { BrandId = 1, ModelName = "X", StatusId = 1 };
            Assert.ThrowsAsync<NotFoundException>(() => _svc.UpdateAsync(404, update));
        }

        [Test]
        public void SearchAsync_Throws_BadRequest_On_Invalid_Date_Range()
        {
            var req = new CarSearchRequestDto
            {
                FromUtc = DateTime.UtcNow.AddDays(2),
                ToUtc = DateTime.UtcNow.AddDays(1)
            };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.SearchAsync(req));
        }

        [Test]
        public async Task SearchAsync_Filters_Out_Overlapping_Bookings_With_Blocking_Status()
        {
            var now = DateTime.UtcNow;
            var cars = new[]
            {
                new Car { CarId=1, BrandId=1, ModelName="Corolla", StatusId=1, DailyRate=50m, Seats=5, Year=2020 },
                new Car { CarId=2, BrandId=2, ModelName="Civic",   StatusId=1, DailyRate=60m, Seats=5, Year=2021 }
            };
            _carRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(cars);

            // Booking for CarId=1 that overlaps the search window, status = Confirmed (2) → should block
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking {
                    BookingId=100, CarId=1, StatusId=2,
                    PickupDateTime = now.AddHours(1),
                    DropoffDateTime = now.AddHours(5)
                }
            });

            var req = new CarSearchRequestDto
            {
                FromUtc = now,
                ToUtc = now.AddHours(3) // overlaps car 1 booking
            };

            var result = (await _svc.SearchAsync(req)).ToList();

            Assert.That(result.Any(c => c.CarId == 1), Is.False); // filtered out
            Assert.That(result.Any(c => c.CarId == 2), Is.True);  // still available
        }

        [Test]
        public async Task EnsureAvailableAsync_Throws_When_Not_Available()
        {
            var now = DateTime.UtcNow;
            _carRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Car { CarId = 10, BrandId = 1, ModelName = "Corolla", StatusId = 1 });

            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking {
                    BookingId=200, CarId=10, StatusId=1, // Pending blocks
                    PickupDateTime = now.AddHours(-1),
                    DropoffDateTime = now.AddHours(2)
                }
            });

            Assert.ThrowsAsync<CarUnavailableException>(() =>
                _svc.EnsureAvailableAsync(10, now, now.AddHours(1)));
        }

        [Test]
        public void SetStatusAsync_Throws_NotFound_On_Bad_Status()
        {
            _carRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Car { CarId = 5, BrandId = 1, ModelName = "X", StatusId = 1 });
            _statusRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CarStatus)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.SetStatusAsync(5, 99));
        }
    }
}
