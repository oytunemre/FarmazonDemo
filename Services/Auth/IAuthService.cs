using FarmazonDemo.Models.Dto;

namespace FarmazonDemo.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        string GenerateJwtToken(int userId, string username, string email, string role);
    }
}
