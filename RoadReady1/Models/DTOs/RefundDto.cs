namespace RoadReady1.Models.DTOs
{
    public class RefundRequestCreateDto
    {
        public int BookingId { get; set; }
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }   // must be > 0 and <= payment.Amount
        public string? Reason { get; set; }
    }

    public class RefundDto
    {
        public int RefundId { get; set; }
        public int BookingId { get; set; }
        public int PaymentId { get; set; }
        public int UserId { get; set; }        // requester (booking owner)
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected/Refunded (we'll use Pending/Rejected/Refunded)
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
