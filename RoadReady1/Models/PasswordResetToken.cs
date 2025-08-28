//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace RoadReady1.Models
//{
//    public class PasswordResetToken
//    {
//        [Key]
//        public int Id { get; set; }

//        [Required]
//        public int UserId { get; set; }

//        [Required]
//        public string Token { get; set; }

//        [Required]
//        public DateTime ExpiresAt { get; set; }

//        public bool Used { get; set; } = false;

//        [ForeignKey(nameof(UserId))]
//        public User User { get; set; }
//    }
//}
