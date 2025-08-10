using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RoadReady1.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey(nameof(Car))]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [ForeignKey(nameof(PickupLocation))]
        public int PickupLocationId { get; set; }
        public Location PickupLocation { get; set; }

        [ForeignKey(nameof(DropoffLocation))]
        public int DropoffLocationId { get; set; }
        public Location DropoffLocation { get; set; }

        public DateTime PickupDateTime { get; set; }
        public DateTime DropoffDateTime { get; set; }

        [ForeignKey(nameof(BookingStatus))]
        public int StatusId { get; set; }
        public BookingStatus BookingStatus { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Precision(10, 2)]
        public decimal TotalAmount { get; set; }

        public ICollection<Payment> Payments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<BookingIssue> BookingIssues { get; set; }
        public ICollection<Refund> Refunds { get; set; }
    }
}