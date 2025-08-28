using AutoMapper;
using RoadReady1.Exceptions;
using RoadReady1.Interfaces;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Services
{
    public class MaintenanceRequestService : IMaintenanceRequestService
    {
        private readonly IRepository<int, MaintenanceRequest> _reqRepo;
        private readonly IRepository<int, Car> _carRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IMapper _mapper;

        public MaintenanceRequestService(
            IRepository<int, MaintenanceRequest> reqRepo,
            IRepository<int, Car> carRepo,
            IRepository<int, Booking> bookingRepo,
            IMapper mapper)
        {
            _reqRepo = reqRepo;
            _carRepo = carRepo;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
        }

        public async Task<MaintenanceRequestDto> CreateAsync(int userId, string role, MaintenanceRequestCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IssueDescription))
                throw new BadRequestException("Issue description is required.");

            _ = await _carRepo.GetByIdAsync(dto.CarId)
                ?? throw new NotFoundException($"Car {dto.CarId} not found");

            // Customers can only report for cars they’ve booked (ongoing or recently).
            var isStaff = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                          role.Equals("RentalAgent", StringComparison.OrdinalIgnoreCase);
            if (!isStaff)
            {
                var allBookings = await _bookingRepo.GetAllAsync();
                var hasRelation = allBookings.Any(b =>
                    b.UserId == userId &&
                    b.CarId == dto.CarId &&
                    (b.PickupDateTime <= DateTime.UtcNow.AddDays(1)) &&
                    (b.DropoffDateTime >= DateTime.UtcNow.AddDays(-30))
                );

                if (!hasRelation)
                    throw new UnauthorizedException("You can only report maintenance for cars you rented recently.");
            }

            var entity = new MaintenanceRequest
            {
                CarId = dto.CarId,
                ReportedById = userId,                    // fk int
                IssueDescription = dto.IssueDescription.Trim(),
                ReportedDate = DateTime.UtcNow,
                IsResolved = false
            };

            entity = await _reqRepo.AddAsync(entity);
            return _mapper.Map<MaintenanceRequestDto>(entity);
        }

        public async Task ResolveAsync(int requestId)
        {
            var req = await _reqRepo.GetByIdAsync(requestId)
                      ?? throw new NotFoundException($"Maintenance request {requestId} not found");
            if (req.IsResolved)
                throw new BadRequestException("Request is already resolved.");

            req.IsResolved = true;
            await _reqRepo.UpdateAsync(requestId, req);
        }

        public async Task<IEnumerable<MaintenanceRequestDto>> GetOpenAsync()
        {
            var all = await _reqRepo.GetAllAsync();
            var open = all.Where(r => !r.IsResolved)
                          .OrderByDescending(r => r.ReportedDate);
            return _mapper.Map<IEnumerable<MaintenanceRequestDto>>(open);
        }

        public async Task<IEnumerable<MaintenanceRequestDto>> GetByCarAsync(int carId)
        {
            var all = await _reqRepo.GetAllAsync();
            var list = all.Where(r => r.CarId == carId)
                          .OrderByDescending(r => r.ReportedDate);
            return _mapper.Map<IEnumerable<MaintenanceRequestDto>>(list);
        }

        public async Task<IEnumerable<MaintenanceRequestDto>> GetMineAsync(int userId)
        {
            var all = await _reqRepo.GetAllAsync();
            var mine = all.Where(r => r.ReportedById == userId)   // <-- FK int (fix)
                          .OrderByDescending(r => r.ReportedDate);
            return _mapper.Map<IEnumerable<MaintenanceRequestDto>>(mine);
        }

        public async Task<MaintenanceRequestDto> GetByIdAsync(int id)
        {
            var req = await _reqRepo.GetByIdAsync(id)
                      ?? throw new NotFoundException($"Maintenance request {id} not found");
            return _mapper.Map<MaintenanceRequestDto>(req);
        }
    }
}
