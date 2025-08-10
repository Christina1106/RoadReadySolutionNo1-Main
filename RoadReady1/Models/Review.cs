using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey(nameof(Car))]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}