using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoadReady1.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required, StringLength(50)]
        public string RoleName { get; set; }

        public ICollection<User> Users { get; set; }
    }
}