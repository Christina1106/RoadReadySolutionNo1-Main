using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoadReady1.Models
{
    public class CarStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required, StringLength(50)]
        public string StatusName { get; set; }

        public ICollection<Car> Cars { get; set; }
    }

}
