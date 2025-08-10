namespace RoadReady1.Models.DTOs
{
    public class ReviewCreateDto
    {
        public int BookingId { get; set; }          // we derive CarId from the booking
        public int Rating { get; set; }             // 1..5
        public string? Comment { get; set; }
    }

    public class ReviewUpdateDto
    {
        public int Rating { get; set; }             // 1..5
        public string? Comment { get; set; }
    }

    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int CarId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
