using System;

namespace RoadReady1.Models.DTOs
{
    /// <summary>
    /// User data for responses (hides password/hash).
    /// </summary>
    public class UserDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}