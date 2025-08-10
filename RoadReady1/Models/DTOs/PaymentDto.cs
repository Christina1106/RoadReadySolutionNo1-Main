namespace RoadReady1.Models.DTOs
{
    // Client provides booking + method.
    // Amount is taken from Booking.TotalAmount.
    public class PaymentCreateDto
    {
        public int BookingId { get; set; }
        public int MethodId { get; set; } // must exist in PaymentMethods
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public int MethodId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = "Success"; // Success/Failed/Refunded
        public string? TransactionId { get; set; }
        public DateTime PaidDate { get; set; }
    }
}
