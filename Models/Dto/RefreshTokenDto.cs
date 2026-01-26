namespace FarmazonDemo.Models.Dto
{
    public class RefreshTokenRequestDto
    {
        public required string RefreshToken { get; set; }
    }

    public class TokenResponseDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
