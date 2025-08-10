namespace RoadReady1.Models.DTOs
{
    public class BookingCreateDto
    {
        public int CarId { get; set; }
        public int PickupLocationId { get; set; }
        public int DropoffLocationId { get; set; }
        public DateTime PickupDateTimeUtc { get; set; }
        public DateTime DropoffDateTimeUtc { get; set; }
    }

    public class BookingDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }

        public int CarId { get; set; }
        public string? CarName { get; set; }           // ModelName only (no brand for now)

        public int PickupLocationId { get; set; }
        public string? PickupLocationName { get; set; }

        public int DropoffLocationId { get; set; }
        public string? DropoffLocationName { get; set; }

        public DateTime PickupDateTimeUtc { get; set; }
        public DateTime DropoffDateTimeUtc { get; set; }

        public int StatusId { get; set; }
        public string? StatusName { get; set; }

        public decimal TotalAmount { get; set; }
    }

    public class BookingQuoteRequestDto
    {
        public int CarId { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
    }

    public class BookingQuoteDto
    {
        public int Days { get; set; }
        public decimal DailyRate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total { get; set; }
    }
}
