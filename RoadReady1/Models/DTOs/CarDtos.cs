namespace RoadReady1.Models.DTOs
{
    public class CarDto
    {
        public int CarId { get; set; }
        public int BrandId { get; set; }
        public string? BrandName { get; set; }
        public string ModelName { get; set; } = default!;
        public int? Year { get; set; }
        public string? FuelType { get; set; }
        public string? Transmission { get; set; }
        public int? Seats { get; set; }
        public decimal? DailyRate { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class CarCreateDto
    {
        public int BrandId { get; set; }
        public string ModelName { get; set; } = default!;
        public int? Year { get; set; }
        public string? FuelType { get; set; }
        public string? Transmission { get; set; }
        public int? Seats { get; set; }
        public decimal? DailyRate { get; set; }
        public int StatusId { get; set; } // e.g., Available
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class CarUpdateDto : CarCreateDto { }

    public class CarSearchRequestDto
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public int? BrandId { get; set; }
        public int? MinSeats { get; set; }
        public string? FuelType { get; set; }
        public string? Transmission { get; set; }
        public decimal? MaxDailyRate { get; set; }
    }

    public class CarStatusUpdateDto
    {
        public int StatusId { get; set; }
    }
}
