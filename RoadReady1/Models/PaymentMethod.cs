using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoadReady1.Models
{
    public class PaymentMethod
    {
        [Key]
        public int MethodId { get; set; }

        [Required, StringLength(50)]
        public string MethodName { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }
}