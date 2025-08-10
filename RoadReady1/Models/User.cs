using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [Required, StringLength(150)]
        public string Email { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [ForeignKey(nameof(Role))]
        public int RoleId { get; set; }
        public Role Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public ICollection<BookingIssue> BookingIssues { get; set; }
        public ICollection<Refund> Refunds { get; set; }
    }
}