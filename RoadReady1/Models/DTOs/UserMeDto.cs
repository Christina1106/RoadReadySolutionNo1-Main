// Models/DTOs/UserMeDto.cs
namespace RoadReady1.Models.DTOs
{
    public class UserMeDto
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? RoleName { get; set; } // "Admin" | "RentalAgent" | "Customer"
    }
}