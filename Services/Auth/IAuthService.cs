using FarmazonDemo.Models.Dto;

namespace FarmazonDemo.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        string GenerateJwtToken(int userId, string username, string email, string role);
        Task<object> GetUserStatsAsync();
        Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);

        // Email Verification
        Task<bool> VerifyEmailAsync(string token);
        Task ResendVerificationEmailAsync(string email);

        // Password Reset
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
    }
}
