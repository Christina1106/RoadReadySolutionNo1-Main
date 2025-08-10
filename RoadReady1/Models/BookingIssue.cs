using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class BookingIssue
    {
        [Key]
        public int IssueId { get; set; }

        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; }

        public string IssueType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}