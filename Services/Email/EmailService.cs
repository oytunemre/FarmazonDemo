using Microsoft.Extensions.Options;

namespace FarmazonDemo.Services.Email
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@farmazon.com";
        public string FromName { get; set; } = "Farmazon";
        public string BaseUrl { get; set; } = "http://localhost:5000";
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _env;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _logger = logger;
            _env = env;
        }

        public async Task SendEmailVerificationAsync(string email, string token)
        {
            var verificationUrl = $"{_settings.BaseUrl}/api/auth/verify-email?token={token}";

            var subject = "Verify your email - Farmazon";
            var body = $@"
                <h2>Welcome to Farmazon!</h2>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationUrl}'>Verify Email</a></p>
                <p>Or copy this link: {verificationUrl}</p>
                <p>This link will expire in 24 hours.</p>
                <br/>
                <p>If you didn't create an account, please ignore this email.</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var resetUrl = $"{_settings.BaseUrl}/reset-password?token={token}";

            var subject = "Reset your password - Farmazon";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>You requested to reset your password. Click the link below:</p>
                <p><a href='{resetUrl}'>Reset Password</a></p>
                <p>Or copy this link: {resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <br/>
                <p>If you didn't request this, please ignore this email.</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendAccountLockedAsync(string email, DateTime lockoutEndTime)
        {
            var subject = "Account Locked - Farmazon";
            var body = $@"
                <h2>Account Temporarily Locked</h2>
                <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
                <p>You can try again after: {lockoutEndTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                <br/>
                <p>If this wasn't you, please reset your password immediately.</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            if (_env.IsDevelopment())
            {
                // Development: Log to console instead of sending
                _logger.LogInformation("========== EMAIL ==========");
                _logger.LogInformation("To: {To}", to);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body: {Body}", htmlBody);
                _logger.LogInformation("===========================");
                await Task.CompletedTask;
                return;
            }

            // Production: Send via SMTP
            try
            {
                using var client = new System.Net.Mail.SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);

                var message = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
