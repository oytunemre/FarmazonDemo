namespace FarmazonDemo.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email, string token);
        Task SendPasswordResetAsync(string email, string token);
        Task SendAccountLockedAsync(string email, DateTime lockoutEndTime);
    }
}
