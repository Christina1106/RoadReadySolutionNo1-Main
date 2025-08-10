using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;


namespace RoadReady1.Models
{

    public class BookingStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required, StringLength(50)]
        public string StatusName { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }

}
