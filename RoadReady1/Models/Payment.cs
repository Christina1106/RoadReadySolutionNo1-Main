using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [ForeignKey(nameof(PaymentMethod))]
        public int MethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        [Precision(10, 2)]
        public decimal Amount { get; set; }

        [Required, StringLength(50)]
        public string PaymentStatus { get; set; }

        public string TransactionId { get; set; }
        public DateTime PaidDate { get; set; } = DateTime.UtcNow;
    }
}
