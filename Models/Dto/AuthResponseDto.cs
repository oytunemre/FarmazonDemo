using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public required string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public required string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
