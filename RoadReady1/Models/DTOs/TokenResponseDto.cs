namespace RoadReady1.Models.DTOs
{
    public class TokenResponseDto
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
