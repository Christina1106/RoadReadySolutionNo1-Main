// File: Mapping/AutoMapperProfile.cs
using AutoMapper;
using RoadReady1.Models;
using RoadReady1.Models.DTOs;

namespace RoadReady1.Mapping
{
    /// <summary>
    /// Central AutoMapper profile.
    /// Add new CreateMap lines here as you build out each module.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserRegisterDto, User>()
               .ForMember(d => d.RoleId, o => o.MapFrom(s => s.RoleId));

            CreateMap<UserUpdateDto, User>();
            // Entity → DTO (for read operations)
            CreateMap<User, UserDto>();
            // Mapping/AutoMapperProfile.cs
            CreateMap<UserCreateDto, User>();


            CreateMap<CarCreateDto, Car>();
            CreateMap<CarUpdateDto, Car>();
            CreateMap<Car, CarDto>(); // BrandName/StatusName filled in service (or use projection if you have navs)


            // Mapping/AutoMapperProfile.cs  (inside the constructor)
            CreateMap<Booking, BookingDto>()
                .ForMember(d => d.PickupDateTimeUtc, o => o.MapFrom(s => s.PickupDateTime))
                .ForMember(d => d.DropoffDateTimeUtc, o => o.MapFrom(s => s.DropoffDateTime));


            // File: Mapping/AutoMapperProfile.cs  (inside your profile constructor)
            CreateMap<BookingIssue, BookingIssueDto>();

            // inside your profile constructor
            CreateMap<Payment, PaymentDto>();
            // File: Mapping/AutoMapperProfile.cs   (inside your profile constructor)
            CreateMap<Refund, RefundDto>();

            // Mappings/AutoMappingProfile.cs (inside ctor)
            CreateMap<Review, ReviewDto>();

            // File: Mappings/AutoMappingProfile.cs  (inside the Profile constructor)
            CreateMap<MaintenanceRequest, MaintenanceRequestDto>();

            CreateMap<MaintenanceRequest, MaintenanceRequestDto>() 
                .ForMember(d => d.ReportedBy, opt => opt.MapFrom(s => s.ReportedById));


           
        }
    }
}
