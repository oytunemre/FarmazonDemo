using FarmazonDemo.Data;
using FarmazonDemo.Models;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Email;
using FarmazonDemo.Services.TwoFactor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FarmazonDemo.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly ITwoFactorService _twoFactorService;

        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutDurationMinutes = 15;

        public AuthService(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService,
            ITwoFactorService twoFactorService)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Check if email already exists
            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingEmail != null)
                throw new ConflictException("Email already exists");

            // Check if username already exists
            var existingUsername = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());

            if (existingUsername != null)
                throw new ConflictException("Username already exists");

            // Hash password using BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Generate email verification token
            var emailVerificationToken = GenerateSecureToken();

            // Create user
            var user = new Models.Entities.Users
            {
                Name = dto.Name,
                Email = dto.Email,
                Username = dto.Username,
                Password = hashedPassword,
                Role = dto.Role,
                EmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send verification email
            await _emailService.SendEmailVerificationAsync(user.Email, emailVerificationToken);

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Username, user.Email, user.Role.ToString());
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            // Generate refresh token
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Find user by email or username
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == dto.EmailOrUsername.ToLower() ||
                    u.Username.ToLower() == dto.EmailOrUsername.ToLower());

            if (user == null)
                throw new NotFoundException("Invalid credentials");

            // Check if account is locked
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
            {
                var remainingMinutes = (int)(user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes + 1;
                throw new UnauthorizedException($"Account is locked. Try again in {remainingMinutes} minutes.");
            }

            // Verify password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

            if (!isPasswordValid)
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                    await _context.SaveChangesAsync();

                    // Send notification email
                    await _emailService.SendAccountLockedAsync(user.Email, user.LockoutEndTime.Value);

                    throw new UnauthorizedException($"Account locked due to {MaxFailedLoginAttempts} failed attempts. Try again in {LockoutDurationMinutes} minutes.");
                }

                await _context.SaveChangesAsync();
                throw new NotFoundException("Invalid credentials");
            }

            // Check if email is verified
            if (!user.EmailVerified)
                throw new UnauthorizedException("Please verify your email before logging in.");

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
            await _context.SaveChangesAsync();

            // Check if 2FA is enabled
            if (user.TwoFactorEnabled)
            {
                return new AuthResponseDto
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role,
                    TwoFactorRequired = true,
                    Token = null!,
                    ExpiresAt = DateTime.MinValue
                };
            }

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Username, user.Email, user.Role.ToString());
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            // Generate refresh token
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task<AuthResponseDto> LoginWith2FAAsync(TwoFactorLoginDto dto)
        {
            // Find user by email or username
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == dto.EmailOrUsername.ToLower() ||
                    u.Username.ToLower() == dto.EmailOrUsername.ToLower());

            if (user == null)
                throw new NotFoundException("Invalid credentials");

            // Check if account is locked
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
            {
                var remainingMinutes = (int)(user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes + 1;
                throw new UnauthorizedException($"Account is locked. Try again in {remainingMinutes} minutes.");
            }

            // Verify password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                    await _context.SaveChangesAsync();
                    await _emailService.SendAccountLockedAsync(user.Email, user.LockoutEndTime.Value);
                    throw new UnauthorizedException($"Account locked due to {MaxFailedLoginAttempts} failed attempts.");
                }

                await _context.SaveChangesAsync();
                throw new NotFoundException("Invalid credentials");
            }

            // Check if email is verified
            if (!user.EmailVerified)
                throw new UnauthorizedException("Please verify your email before logging in.");

            // Verify 2FA is enabled
            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                throw new BadRequestException("2FA is not enabled for this account. Use regular login.");

            // Verify 2FA code
            var isValidCode = _twoFactorService.ValidateCode(user.TwoFactorSecretKey, dto.TwoFactorCode);

            if (!isValidCode)
            {
                // Check backup codes
                var backupCodes = user.TwoFactorBackupCodes?.Split(',').ToList() ?? new List<string>();
                if (backupCodes.Contains(dto.TwoFactorCode))
                {
                    // Remove used backup code
                    backupCodes.Remove(dto.TwoFactorCode);
                    user.TwoFactorBackupCodes = string.Join(",", backupCodes);
                }
                else
                {
                    throw new UnauthorizedException("Invalid 2FA code");
                }
            }

            // Reset failed login attempts
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Username, user.Email, user.Role.ToString());
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            // Generate refresh token
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public string GenerateJwtToken(int userId, string username, string email, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<object> GetUserStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var customerCount = await _context.Users.CountAsync(u => u.Role == Models.Enums.UserRole.Customer);
            var sellerCount = await _context.Users.CountAsync(u => u.Role == Models.Enums.UserRole.Seller);
            var adminCount = await _context.Users.CountAsync(u => u.Role == Models.Enums.UserRole.Admin);

            return new
            {
                TotalUsers = totalUsers,
                CustomerCount = customerCount,
                SellerCount = sellerCount,
                AdminCount = adminCount
            };
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null)
                throw new UnauthorizedException("Invalid refresh token");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedException("Refresh token expired");
            }

            var user = storedToken.User;

            // Revoke old refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var newAccessToken = GenerateJwtToken(user.Id, user.Username, user.Email, user.Role.ToString());
            var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
                throw new NotFoundException("Refresh token not found");

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshTokenString(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                UserId = userId
            };

            _context.RefreshTokens.Add(refreshToken);
            return refreshToken;
        }

        private static string GenerateRefreshTokenString()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToHexString(randomBytes).ToLower();
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
                throw new NotFoundException("Invalid verification token");

            if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
                throw new BadRequestException("Verification token has expired. Please request a new one.");

            if (user.EmailVerified)
                throw new BadRequestException("Email is already verified");

            user.EmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ResendVerificationEmailAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
                throw new NotFoundException("User not found");

            if (user.EmailVerified)
                throw new BadRequestException("Email is already verified");

            // Generate new token
            user.EmailVerificationToken = GenerateSecureToken();
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();
            await _emailService.SendEmailVerificationAsync(user.Email, user.EmailVerificationToken);
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            // Don't reveal if user exists or not for security
            if (user == null)
                return;

            // Generate password reset token
            user.PasswordResetToken = GenerateSecureToken();
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();
            await _emailService.SendPasswordResetAsync(user.Email, user.PasswordResetToken);
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null)
                throw new NotFoundException("Invalid reset token");

            if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
                throw new BadRequestException("Reset token has expired. Please request a new one.");

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            // Reset lockout on password change
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;

            await _context.SaveChangesAsync();
        }
    }
}
