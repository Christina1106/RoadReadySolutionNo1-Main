// File: Services/CarService.cs
using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class CarService : ICarService
    {
        private readonly IRepository<int, Car> _carRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IRepository<int, CarBrand> _brandRepo;
        private readonly IRepository<int, CarStatus> _statusRepo;
        private readonly IMapper _mapper;

        // Pending(1), Confirmed(2), CheckedOut(4) block availability
        private static readonly HashSet<int> BlockingStatuses = new() { 1, 2, 4 };

        public CarService(
            IRepository<int, Car> carRepo,
            IRepository<int, Booking> bookingRepo,
            IRepository<int, CarBrand> brandRepo,
            IRepository<int, CarStatus> statusRepo,
            IMapper mapper)
        {
            _carRepo = carRepo;
            _bookingRepo = bookingRepo;
            _brandRepo = brandRepo;
            _statusRepo = statusRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CarDto>> GetAllAsync()
        {
            var cars = await _carRepo.GetAllAsync();
            return await EnrichAndMapAsync(cars);
        }

        public async Task<CarDto> GetByIdAsync(int id)
        {
            var car = await _carRepo.GetByIdAsync(id);
            if (car == null) throw new NotFoundException($"Car {id} not found");
            return (await EnrichAndMapAsync(new[] { car })).First();
        }

        public async Task<CarDto> CreateAsync(CarCreateDto dto)
        {
            if (await _brandRepo.GetByIdAsync(dto.BrandId) == null)
                throw new NotFoundException($"Brand {dto.BrandId} not found");
            if (await _statusRepo.GetByIdAsync(dto.StatusId) == null)
                throw new NotFoundException($"Car status {dto.StatusId} not found");

            var dupe = await _carRepo.FindAsync(c =>
                c.BrandId == dto.BrandId &&
                c.ModelName == dto.ModelName &&
                c.Year == dto.Year);
            if (dupe != null)
                throw new BadRequestException("A car with the same brand/model/year already exists.");

            var entity = _mapper.Map<Car>(dto);
            var created = await _carRepo.AddAsync(entity);
            return (await EnrichAndMapAsync(new[] { created })).First();
        }

        public async Task<CarDto> UpdateAsync(int id, CarUpdateDto dto)
        {
            var entity = await _carRepo.GetByIdAsync(id);
            if (entity == null) throw new NotFoundException($"Car {id} not found");

            if (await _brandRepo.GetByIdAsync(dto.BrandId) == null)
                throw new NotFoundException($"Brand {dto.BrandId} not found");
            if (await _statusRepo.GetByIdAsync(dto.StatusId) == null)
                throw new NotFoundException($"Car status {dto.StatusId} not found");

            _mapper.Map(dto, entity);
            var updated = await _carRepo.UpdateAsync(id, entity);
            return (await EnrichAndMapAsync(new[] { updated })).First();
        }

        public async Task SetStatusAsync(int id, int statusId)
        {
            var car = await _carRepo.GetByIdAsync(id);
            if (car == null) throw new NotFoundException($"Car {id} not found");
            if (await _statusRepo.GetByIdAsync(statusId) == null)
                throw new NotFoundException($"Car status {statusId} not found");

            car.StatusId = statusId;
            await _carRepo.UpdateAsync(id, car);
        }

        public async Task DeleteAsync(int id)
        {
            var car = await _carRepo.GetByIdAsync(id);
            if (car == null) throw new NotFoundException($"Car {id} not found");
            await _carRepo.DeleteAsync(id);
        }

        public async Task<IEnumerable<CarDto>> SearchAsync(CarSearchRequestDto req)
        {
            if (req.ToUtc <= req.FromUtc)
                throw new BadRequestException("ToUtc must be after FromUtc");

            var cars = await _carRepo.GetAllAsync();

            if (req.BrandId.HasValue)
                cars = cars.Where(c => c.BrandId == req.BrandId.Value);
            if (!string.IsNullOrWhiteSpace(req.FuelType))
                cars = cars.Where(c => c.FuelType == req.FuelType);
            if (!string.IsNullOrWhiteSpace(req.Transmission))
                cars = cars.Where(c => c.Transmission == req.Transmission);
            if (req.MinSeats.HasValue)
                cars = cars.Where(c => c.Seats >= req.MinSeats.Value);      // Seats is int
            if (req.MaxDailyRate.HasValue)
                cars = cars.Where(c => c.DailyRate <= req.MaxDailyRate.Value);

            var bookings = await _bookingRepo.GetAllAsync();
            var available = cars.Where(car =>
                !bookings.Any(b =>
                    b.CarId == car.CarId &&
                    BlockingStatuses.Contains(b.StatusId) &&
                    b.PickupDateTime < req.ToUtc &&
                    b.DropoffDateTime > req.FromUtc));

            return await EnrichAndMapAsync(available);
        }

        public async Task<bool> IsAvailableAsync(int carId, DateTime fromUtc, DateTime toUtc)
        {
            if (toUtc <= fromUtc) throw new BadRequestException("ToUtc must be after FromUtc");

            var car = await _carRepo.GetByIdAsync(carId);
            if (car == null) throw new NotFoundException($"Car {carId} not found");

            var bookings = await _bookingRepo.GetAllAsync();
            var overlap = bookings.Any(b =>
                b.CarId == carId &&
                BlockingStatuses.Contains(b.StatusId) &&
                b.PickupDateTime < toUtc &&
                b.DropoffDateTime > fromUtc);

            return !overlap;
        }

        public async Task EnsureAvailableAsync(int carId, DateTime fromUtc, DateTime toUtc)
        {
            if (!await IsAvailableAsync(carId, fromUtc, toUtc))
                throw new CarUnavailableException(carId);
        }

        public async Task<IEnumerable<CarDto>> SearchByBrandAsync(string brandName)
        {
            if (string.IsNullOrWhiteSpace(brandName))
                return Enumerable.Empty<CarDto>();

            var brands = await _brandRepo.GetAllAsync();
            var matchedBrandIds = brands
                .Where(b => b.BrandName.Contains(brandName, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.BrandId)
                .ToHashSet();

            if (matchedBrandIds.Count == 0)
                return Enumerable.Empty<CarDto>();

            var cars = await _carRepo.GetAllAsync();
            var filtered = cars.Where(c => matchedBrandIds.Contains(c.BrandId));
            return await EnrichAndMapAsync(filtered);
        }

        // ----- helpers -----
        private async Task<IEnumerable<CarDto>> EnrichAndMapAsync(IEnumerable<Car> cars)
        {
            var list = cars.ToList();
            var brands = (await _brandRepo.GetAllAsync()).ToDictionary(b => b.BrandId, b => b.BrandName);
            var statuses = (await _statusRepo.GetAllAsync()).ToDictionary(s => s.StatusId, s => s.StatusName);

            return list.Select(c =>
            {
                var dto = _mapper.Map<CarDto>(c);
                dto.BrandName = brands.TryGetValue(c.BrandId, out var bn) ? bn : null;
                dto.StatusName = statuses.TryGetValue(c.StatusId, out var sn) ? sn : null;
                return dto;
            });
        }
    }
}
