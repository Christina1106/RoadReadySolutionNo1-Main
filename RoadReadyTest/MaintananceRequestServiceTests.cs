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
    public class MaintenanceRequestServiceTests
    {
        private Mock<IRepository<int, MaintenanceRequest>> _reqRepo = null!;
        private Mock<IRepository<int, Car>> _carRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private MaintenanceRequestService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _reqRepo = new Mock<IRepository<int, MaintenanceRequest>>();
            _carRepo = new Mock<IRepository<int, Car>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _mapper = new Mock<IMapper>();

            _mapper.Setup(m => m.Map<MaintenanceRequestDto>(It.IsAny<MaintenanceRequest>()))
                   .Returns((MaintenanceRequest e) => new MaintenanceRequestDto
                   {
                       RequestId = e.RequestId,
                       CarId = e.CarId,
                       ReportedBy = e.ReportedById, // dto uses ReportedBy mapped from ReportedById
                       IssueDescription = e.IssueDescription,
                       ReportedDate = e.ReportedDate,
                       IsResolved = e.IsResolved
                   });

            _mapper.Setup(m => m.Map<IEnumerable<MaintenanceRequestDto>>(It.IsAny<IEnumerable<MaintenanceRequest>>()))
                   .Returns((IEnumerable<MaintenanceRequest> list) => list.Select(e => new MaintenanceRequestDto
                   {
                       RequestId = e.RequestId,
                       CarId = e.CarId,
                       ReportedBy = e.ReportedById,
                       IssueDescription = e.IssueDescription,
                       ReportedDate = e.ReportedDate,
                       IsResolved = e.IsResolved
                   }).ToList());

            _svc = new MaintenanceRequestService(_reqRepo.Object, _carRepo.Object, _bookingRepo.Object, _mapper.Object);
        }

        // -------- Create --------

        [Test]
        public void CreateAsync_Throws_When_Description_Missing()
        {
            var dto = new MaintenanceRequestCreateDto { CarId = 1, IssueDescription = "   " };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(10, "Customer", dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Car_Not_Found()
        {
            _carRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Car)null!);
            var dto = new MaintenanceRequestCreateDto { CarId = 9, IssueDescription = "Engine noise" };

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(10, "Customer", dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Customer_Has_No_Related_Booking()
        {
            _carRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Car { CarId = 7 });
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Booking>());

            var dto = new MaintenanceRequestCreateDto { CarId = 7, IssueDescription = "Flat tire" };
            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.CreateAsync(10, "Customer", dto));
        }

        [Test]
        public async Task CreateAsync_Succeeds_For_Customer_With_Recent_Booking()
        {
            _carRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Car { CarId = 7 });
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Booking {
                    BookingId = 1, UserId = 10, CarId = 7,
                    PickupDateTime = DateTime.UtcNow.AddDays(-2),
                    DropoffDateTime = DateTime.UtcNow.AddDays(-1)
                }
            });

            _reqRepo.Setup(r => r.AddAsync(It.IsAny<MaintenanceRequest>()))
                    .ReturnsAsync((MaintenanceRequest e) => { e.RequestId = 123; return e; });

            var dto = new MaintenanceRequestCreateDto { CarId = 7, IssueDescription = "Oil leak" };
            var result = await _svc.CreateAsync(10, "Customer", dto);

            Assert.That(result.RequestId, Is.EqualTo(123));
            Assert.That(result.CarId, Is.EqualTo(7));
            Assert.That(result.ReportedBy, Is.EqualTo(10));
            Assert.That(result.IsResolved, Is.False);
        }

        [Test]
        public async Task CreateAsync_Succeeds_For_Staff_Without_Relation()
        {
            _carRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Car { CarId = 7 });
            _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Booking>());

            _reqRepo.Setup(r => r.AddAsync(It.IsAny<MaintenanceRequest>()))
                    .ReturnsAsync((MaintenanceRequest e) => { e.RequestId = 456; return e; });

            var dto = new MaintenanceRequestCreateDto { CarId = 7, IssueDescription = "Brake issue" };
            var result = await _svc.CreateAsync(99, "Admin", dto);

            Assert.That(result.RequestId, Is.EqualTo(456));
        }

        // -------- Resolve --------

        [Test]
        public void ResolveAsync_Throws_When_Not_Found()
        {
            _reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MaintenanceRequest)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.ResolveAsync(1));
        }

        [Test]
        public void ResolveAsync_Throws_When_Already_Resolved()
        {
            _reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new MaintenanceRequest { RequestId = 1, IsResolved = true });
            Assert.ThrowsAsync<BadRequestException>(() => _svc.ResolveAsync(1));
        }

        [Test]
        public async Task ResolveAsync_Sets_IsResolved_True()
        {
            var entity = new MaintenanceRequest { RequestId = 1, IsResolved = false };
            _reqRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _reqRepo.Setup(r => r.UpdateAsync(1, It.IsAny<MaintenanceRequest>()))
                    .ReturnsAsync((int _, MaintenanceRequest e) => e);

            await _svc.ResolveAsync(1);

            _reqRepo.Verify(r => r.UpdateAsync(1, It.Is<MaintenanceRequest>(x => x.IsResolved)), Times.Once);
        }

        // -------- Queries --------

        [Test]
        public async Task GetOpenAsync_Returns_Unresolved_Sorted_Desc()
        {
            _reqRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new MaintenanceRequest { RequestId=1, IsResolved=false, ReportedDate=DateTime.UtcNow.AddMinutes(-10) },
                new MaintenanceRequest { RequestId=2, IsResolved=true,  ReportedDate=DateTime.UtcNow.AddMinutes(-5)  },
                new MaintenanceRequest { RequestId=3, IsResolved=false, ReportedDate=DateTime.UtcNow.AddMinutes(-1)  }
            });

            var list = (await _svc.GetOpenAsync()).ToList();
            Assert.That(list.Select(x => x.RequestId), Is.EqualTo(new[] { 3, 1 }));
        }

        [Test]
        public async Task GetByCarAsync_Filters_By_Car()
        {
            _reqRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new MaintenanceRequest { RequestId=1, CarId=7, ReportedDate=DateTime.UtcNow.AddMinutes(-2) },
                new MaintenanceRequest { RequestId=2, CarId=8, ReportedDate=DateTime.UtcNow.AddMinutes(-1) },
                new MaintenanceRequest { RequestId=3, CarId=7, ReportedDate=DateTime.UtcNow.AddMinutes(-3) }
            });

            var list = (await _svc.GetByCarAsync(7)).ToList();
            Assert.That(list.Select(x => x.RequestId), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public async Task GetMineAsync_Filters_By_ReportedById()
        {
            _reqRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new MaintenanceRequest { RequestId=1, ReportedById=10, ReportedDate=DateTime.UtcNow.AddMinutes(-1) },
                new MaintenanceRequest { RequestId=2, ReportedById=11, ReportedDate=DateTime.UtcNow.AddMinutes(-2) },
                new MaintenanceRequest { RequestId=3, ReportedById=10, ReportedDate=DateTime.UtcNow.AddMinutes(-3) }
            });

            var list = (await _svc.GetMineAsync(10)).ToList();
            Assert.That(list.Select(x => x.RequestId), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public async Task GetByIdAsync_Returns_Dto()
        {
            _reqRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new MaintenanceRequest { RequestId = 5, CarId = 2 });
            var dto = await _svc.GetByIdAsync(5);
            Assert.That(dto.RequestId, Is.EqualTo(5));
            Assert.That(dto.CarId, Is.EqualTo(2));
        }

        [Test]
        public void GetByIdAsync_Throws_NotFound()
        {
            _reqRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((MaintenanceRequest)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(404));
        }
    }
}
