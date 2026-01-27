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
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public bool TwoFactorRequired { get; set; } = false;
    }
}
