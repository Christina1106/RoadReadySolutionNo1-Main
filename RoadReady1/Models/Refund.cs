using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class Refund
    {
        [Key]
        public int RefundId { get; set; }

        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [ForeignKey(nameof(Payment))]
        public int PaymentId { get; set; }
        public Payment Payment { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; }

        [Precision(10, 2)]
        public decimal Amount { get; set; }

        public string Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}

