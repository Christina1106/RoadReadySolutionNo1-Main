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
    public class ReviewServiceTests
    {
        private Mock<IRepository<int, Review>> _reviewRepo = null!;
        private Mock<IRepository<int, Booking>> _bookingRepo = null!;
        private Mock<IMapper> _mapper = null!;
        private ReviewService _svc = null!;

        [SetUp]
        public void SetUp()
        {
            _reviewRepo = new Mock<IRepository<int, Review>>();
            _bookingRepo = new Mock<IRepository<int, Booking>>();
            _mapper = new Mock<IMapper>();

            // Map Review -> ReviewDto
            _mapper.Setup(m => m.Map<ReviewDto>(It.IsAny<Review>()))
                   .Returns((Review r) => new ReviewDto
                   {
                       ReviewId = r.ReviewId,
                       BookingId = r.BookingId,
                       UserId = r.UserId,
                       CarId = r.CarId,
                       Rating = r.Rating,
                       Comment = r.Comment,
                       CreatedDate = r.CreatedDate
                   });

            // Map IEnumerable<Review> -> IEnumerable<ReviewDto>
            _mapper.Setup(m => m.Map<IEnumerable<ReviewDto>>(It.IsAny<IEnumerable<Review>>()))
                   .Returns((IEnumerable<Review> list) => list.Select(r => new ReviewDto
                   {
                       ReviewId = r.ReviewId,
                       BookingId = r.BookingId,
                       UserId = r.UserId,
                       CarId = r.CarId,
                       Rating = r.Rating,
                       Comment = r.Comment,
                       CreatedDate = r.CreatedDate
                   }).ToList());

            _svc = new ReviewService(_reviewRepo.Object, _bookingRepo.Object, _mapper.Object);
        }

        // -------- Create --------

        [Test]
        public void CreateAsync_Throws_When_Rating_Out_Of_Range()
        {
            var dto = new ReviewCreateDto { BookingId = 1, Rating = 6, Comment = "too high" };
            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Booking_Not_Found()
        {
            var dto = new ReviewCreateDto { BookingId = 99, Rating = 5, Comment = "ok" };
            _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking)null!);

            Assert.ThrowsAsync<NotFoundException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Not_Owner()
        {
            var dto = new ReviewCreateDto { BookingId = 5, Rating = 4, Comment = "ok" };
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking
            {
                BookingId = 5,
                UserId = 42,
                CarId = 7,
                DropoffDateTime = DateTime.UtcNow.AddDays(-1)
            });

            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Trip_Not_Ended()
        {
            var dto = new ReviewCreateDto { BookingId = 5, Rating = 4, Comment = "early" };
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking
            {
                BookingId = 5,
                UserId = 10,
                CarId = 7,
                DropoffDateTime = DateTime.UtcNow.AddHours(2)
            });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public void CreateAsync_Throws_When_Duplicate_For_Booking_And_User()
        {
            var dto = new ReviewCreateDto { BookingId = 5, Rating = 4, Comment = "dup" };
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking
            {
                BookingId = 5,
                UserId = 10,
                CarId = 7,
                DropoffDateTime = DateTime.UtcNow.AddHours(-2)
            });

            _reviewRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                       .ReturnsAsync(new Review { ReviewId = 11 });

            Assert.ThrowsAsync<BadRequestException>(() => _svc.CreateAsync(10, dto));
        }

        [Test]
        public async Task CreateAsync_Succeeds()
        {
            var dto = new ReviewCreateDto { BookingId = 5, Rating = 5, Comment = "great" };
            _bookingRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Booking
            {
                BookingId = 5,
                UserId = 10,
                CarId = 7,
                DropoffDateTime = DateTime.UtcNow.AddHours(-1)
            });

            _reviewRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                       .ReturnsAsync((Review)null!);

            _reviewRepo.Setup(r => r.AddAsync(It.IsAny<Review>()))
                       .ReturnsAsync((Review e) => { e.ReviewId = 1001; return e; });

            var result = await _svc.CreateAsync(10, dto);

            Assert.That(result.ReviewId, Is.EqualTo(1001));
            Assert.That(result.BookingId, Is.EqualTo(5));
            Assert.That(result.CarId, Is.EqualTo(7));
            Assert.That(result.Rating, Is.EqualTo(5));
            Assert.That(result.Comment, Is.EqualTo("great"));
        }

        // -------- Update --------

        [Test]
        public void UpdateAsync_Throws_When_Not_Found()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Review)null!);
            var dto = new ReviewUpdateDto { Rating = 4, Comment = "edit" };

            Assert.ThrowsAsync<NotFoundException>(() => _svc.UpdateAsync(10, 77, dto));
        }

        [Test]
        public void UpdateAsync_Throws_When_Not_Owner()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync(new Review { ReviewId = 77, UserId = 99, Rating = 3 });
            var dto = new ReviewUpdateDto { Rating = 4, Comment = "edit" };

            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.UpdateAsync(10, 77, dto));
        }

        [Test]
        public void UpdateAsync_Throws_When_Rating_Invalid()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync(new Review { ReviewId = 77, UserId = 10, Rating = 3 });
            var dto = new ReviewUpdateDto { Rating = 0, Comment = "bad" };

            Assert.ThrowsAsync<BadRequestException>(() => _svc.UpdateAsync(10, 77, dto));
        }

        [Test]
        public async Task UpdateAsync_Succeeds()
        {
            var entity = new Review { ReviewId = 77, UserId = 10, Rating = 3, Comment = "old" };
            _reviewRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync(entity);

            _reviewRepo.Setup(r => r.UpdateAsync(77, It.IsAny<Review>()))
                       .ReturnsAsync((int _, Review e) => e);

            var dto = new ReviewUpdateDto { Rating = 5, Comment = "new" };
            var result = await _svc.UpdateAsync(10, 77, dto);

            Assert.That(result.Rating, Is.EqualTo(5));
            Assert.That(result.Comment, Is.EqualTo("new"));
        }

        // -------- Delete --------

        [Test]
        public void DeleteAsync_Throws_When_Not_Found()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Review)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.DeleteAsync(10, "Customer", 9));
        }

        [Test]
        public void DeleteAsync_Throws_When_Not_Owner_Or_Staff()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Review { ReviewId = 9, UserId = 99 });
            Assert.ThrowsAsync<UnauthorizedException>(() => _svc.DeleteAsync(10, "Customer", 9));
        }

        [Test]
        public async Task DeleteAsync_Succeeds_For_Owner()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Review { ReviewId = 9, UserId = 10 });
            _reviewRepo.Setup(r => r.DeleteAsync(9)).ReturnsAsync(new Review { ReviewId = 9 });

            await _svc.DeleteAsync(10, "Customer", 9);

            _reviewRepo.Verify(r => r.DeleteAsync(9), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_Succeeds_For_Staff()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Review { ReviewId = 9, UserId = 123 });
            _reviewRepo.Setup(r => r.DeleteAsync(9)).ReturnsAsync(new Review { ReviewId = 9 });

            await _svc.DeleteAsync(10, "Admin", 9);

            _reviewRepo.Verify(r => r.DeleteAsync(9), Times.Once);
        }

        // -------- Queries --------

        [Test]
        public async Task GetByCarAsync_Filters_By_Car_And_Sorts()
        {
            _reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Review { ReviewId=1, CarId=7, CreatedDate=DateTime.UtcNow.AddMinutes(-10) },
                new Review { ReviewId=2, CarId=8, CreatedDate=DateTime.UtcNow.AddMinutes(-5) },
                new Review { ReviewId=3, CarId=7, CreatedDate=DateTime.UtcNow.AddMinutes(-1) },
            });

            var list = (await _svc.GetByCarAsync(7)).ToList();
            Assert.That(list.Select(x => x.ReviewId), Is.EqualTo(new[] { 3, 1 }));
        }

        [Test]
        public async Task GetMineAsync_Filters_By_User_And_Sorts()
        {
            _reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
            {
                new Review { ReviewId=1, UserId=10, CreatedDate=DateTime.UtcNow.AddMinutes(-2) },
                new Review { ReviewId=2, UserId=11, CreatedDate=DateTime.UtcNow.AddMinutes(-3) },
                new Review { ReviewId=3, UserId=10, CreatedDate=DateTime.UtcNow.AddMinutes(-1) },
            });

            var list = (await _svc.GetMineAsync(10)).ToList();
            Assert.That(list.Select(x => x.ReviewId), Is.EqualTo(new[] { 3, 1 }));
        }

        [Test]
        public async Task GetByIdAsync_Returns_Dto()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Review { ReviewId = 5, Rating = 4 });
            var dto = await _svc.GetByIdAsync(5);
            Assert.That(dto.ReviewId, Is.EqualTo(5));
        }

        [Test]
        public void GetByIdAsync_Throws_NotFound()
        {
            _reviewRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Review)null!);
            Assert.ThrowsAsync<NotFoundException>(() => _svc.GetByIdAsync(404));
        }
    }
}
