using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;
using System.Globalization;

namespace RoadReady1.Services
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IRepository<int, Car> _carRepo;
        private readonly IRepository<int, BookingStatus> _bookingStatusRepo;
        private readonly IRepository<int, Location> _locationRepo;
        private readonly ICarService _carService;
        private readonly IMapper _mapper;

        public BookingService(
            IRepository<int, Booking> bookingRepo,
            IRepository<int, Car> carRepo,
            IRepository<int, BookingStatus> bookingStatusRepo,
            IRepository<int, Location> locationRepo,
            ICarService carService,
            IMapper mapper)
        {
            _bookingRepo = bookingRepo;
            _carRepo = carRepo;
            _bookingStatusRepo = bookingStatusRepo;
            _locationRepo = locationRepo;
            _carService = carService;
            _mapper = mapper;
        }

        public async Task<BookingQuoteDto> QuoteAsync(BookingQuoteRequestDto req)
        {
            if (req.ToUtc <= req.FromUtc)
                throw new BadRequestException("ToUtc must be after FromUtc");

            var car = await _carRepo.GetByIdAsync(req.CarId)
                      ?? throw new NotFoundException($"Car {req.CarId} not found");

            decimal daily = car.DailyRate; // if nullable in your model, use: car.DailyRate ?? 0m;
            if (daily <= 0) throw new BadRequestException("Daily rate not set for this car.");

            var days = Math.Max(1, (int)Math.Ceiling((req.ToUtc - req.FromUtc).TotalDays));
            var subtotal = daily * days;
            var taxes = Math.Round(subtotal * 0.12m, 2); // sample tax rate
            var total = subtotal + taxes;

            return new BookingQuoteDto
            {
                Days = days,
                DailyRate = daily,
                Subtotal = subtotal,
                Taxes = taxes,
                Total = total
            };
        }

        public async Task<BookingDto> CreateAsync(int userId, BookingCreateDto dto)
        {
            if (dto.DropoffDateTimeUtc <= dto.PickupDateTimeUtc)
                throw new BadRequestException("Dropoff must be after pickup");

            var car = await _carRepo.GetByIdAsync(dto.CarId)
                      ?? throw new NotFoundException($"Car {dto.CarId} not found");
            _ = await _locationRepo.GetByIdAsync(dto.PickupLocationId)
                ?? throw new NotFoundException($"Pickup location {dto.PickupLocationId} not found");
            _ = await _locationRepo.GetByIdAsync(dto.DropoffLocationId)
                ?? throw new NotFoundException($"Dropoff location {dto.DropoffLocationId} not found");

            // ensure availability
            await _carService.EnsureAvailableAsync(dto.CarId, dto.PickupDateTimeUtc, dto.DropoffDateTimeUtc);

            // price
            var quote = await QuoteAsync(new BookingQuoteRequestDto
            {
                CarId = dto.CarId,
                FromUtc = dto.PickupDateTimeUtc,
                ToUtc = dto.DropoffDateTimeUtc
            });

            // status: find 'pending' by name (no magic IDs)
            var pending = await _bookingStatusRepo.FindAsync(s => s.StatusName.ToLower() == "pending");
            if (pending == null) throw new NotFoundException("BookingStatus 'pending' not found");

            var booking = new Booking
            {
                UserId = userId,
                CarId = dto.CarId,
                PickupLocationId = dto.PickupLocationId,
                DropoffLocationId = dto.DropoffLocationId,
                PickupDateTime = dto.PickupDateTimeUtc,
                DropoffDateTime = dto.DropoffDateTimeUtc,
                StatusId = pending.StatusId,
                BookingDate = DateTime.UtcNow,
                TotalAmount = quote.Total
            };

            var created = await _bookingRepo.AddAsync(booking);
            return await ToDtoAsync(created);
        }

        public async Task<IEnumerable<BookingDto>> GetMineAsync(int userId)
        {
            var all = await _bookingRepo.GetAllAsync();
            var mine = all.Where(b => b.UserId == userId)
                          .OrderByDescending(b => b.BookingDate);
            return await ToDtoListAsync(mine);
        }

        public async Task<IEnumerable<BookingDto>> GetAllAsync()
        {
            var all = await _bookingRepo.GetAllAsync();
            return await ToDtoListAsync(all.OrderByDescending(b => b.BookingDate));
        }

        public async Task<BookingDto> GetByIdAsync(int id)
        {
            var b = await _bookingRepo.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Booking {id} not found");
            return await ToDtoAsync(b);
        }

        public async Task CancelAsync(int userId, int bookingId)
        {
            var b = await _bookingRepo.GetByIdAsync(bookingId)
                    ?? throw new NotFoundException($"Booking {bookingId} not found");

            if (b.UserId != userId)
                throw new UnauthorizedException();

            if (DateTime.UtcNow >= b.PickupDateTime)
                throw new BadRequestException("Cannot cancel on/after pickup time.");

            var cancelled = await _bookingStatusRepo.FindAsync(s => s.StatusName.ToLower() == "cancelled");
            if (cancelled == null) throw new NotFoundException("BookingStatus 'cancelled' not found");

            b.StatusId = cancelled.StatusId;
            await _bookingRepo.UpdateAsync(b.BookingId, b);
        }

        // ----- helpers -----
        private async Task<BookingDto> ToDtoAsync(Booking b)
        {
            var dto = _mapper.Map<BookingDto>(b);

            var car = await _carRepo.GetByIdAsync(b.CarId);
            dto.CarName = car?.ModelName;

            var pick = await _locationRepo.GetByIdAsync(b.PickupLocationId);
            var drop = await _locationRepo.GetByIdAsync(b.DropoffLocationId);
            dto.PickupLocationName = pick?.LocationName;
            dto.DropoffLocationName = drop?.LocationName;

            var status = await _bookingStatusRepo.GetByIdAsync(b.StatusId);
            dto.StatusName = status?.StatusName;

            dto.PickupDateTimeUtc = b.PickupDateTime;
            dto.DropoffDateTimeUtc = b.DropoffDateTime;

            return dto;
        }

        private async Task<IEnumerable<BookingDto>> ToDtoListAsync(IEnumerable<Booking> src)
        {
            var list = new List<BookingDto>();
            foreach (var b in src)
                list.Add(await ToDtoAsync(b));
            return list;
        }
    }
}
