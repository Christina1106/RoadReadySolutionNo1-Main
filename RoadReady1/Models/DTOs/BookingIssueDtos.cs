namespace RoadReady1.Models.DTOs
{
    public class BookingIssueCreateDto
    {
        public int BookingId { get; set; }
        public string? IssueType { get; set; }     // e.g., "Billing", "Car Issue"
        public string Description { get; set; } = string.Empty;
    }

    public class BookingIssueDto
    {
        public int IssueId { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string? IssueType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";   // Open, In Progress, Resolved, Closed
        public DateTime CreatedAt { get; set; }
    }

    public class BookingIssueStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty; // Open/In Progress/Resolved/Closed
    }
}
