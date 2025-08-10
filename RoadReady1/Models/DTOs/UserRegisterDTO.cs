namespace RoadReady1.Models.DTOs
{
    public class UserRegisterDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; } // e.g., 3 = Customer, 2 = RentalAgent
    }
}

