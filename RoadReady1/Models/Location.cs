using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoadReady1.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required, StringLength(100)]
        public string LocationName { get; set; }

        public string Address { get; set; }

        public ICollection<Booking> Pickups { get; set; }
        public ICollection<Booking> Dropoffs { get; set; }
    }
}