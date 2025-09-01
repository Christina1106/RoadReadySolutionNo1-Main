namespace RoadReady1.Models.DTOs
{
    /// <summary>
    /// Data needed to update a user's profile.
    /// </summary>
    public class UserUpdateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}