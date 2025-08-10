using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoadReady1.Models
{
    public class CarBrand
    {
        [Key]
        public int BrandId { get; set; }

        [Required, StringLength(100)]
        public string BrandName { get; set; }

        public ICollection<Car> Cars { get; set; }
    }
}
