using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        [ForeignKey(nameof(Car))]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [ForeignKey(nameof(ReportedBy))]
        public int ReportedById { get; set; }
        public User ReportedBy { get; set; }

        [Required]
        public string IssueDescription { get; set; }

        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
    }
}