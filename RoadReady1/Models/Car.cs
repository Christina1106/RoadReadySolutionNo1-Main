using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class Car
    {
        [Key]
        public int CarId { get; set; }

        [ForeignKey(nameof(CarBrand))]
        public int BrandId { get; set; }
        public CarBrand CarBrand { get; set; }

        [Required, StringLength(100)]
        public string ModelName { get; set; }

        public int? Year { get; set; }
        public string FuelType { get; set; }
        public string Transmission { get; set; }
        public int? Seats { get; set; }

        [Precision(10, 2)]
        public decimal DailyRate { get; set; }

        [ForeignKey(nameof(CarStatus))]
        public int StatusId { get; set; }
        public CarStatus CarStatus { get; set; }

        public string ImageUrl { get; set; }
        public string Description { get; set; }

        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public ICollection<Refund> Refunds { get; set; }
    }
}
