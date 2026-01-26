using FarmazonDemo.Data;
using FarmazonDemo.Models;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
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

        public AuthService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
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

            // Create user
            var user = new Models.Entities.Users
            {
                Name = dto.Name,
                Email = dto.Email,
                Username = dto.Username,
                Password = hashedPassword,
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

            // Verify password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

            if (!isPasswordValid)
                throw new NotFoundException("Invalid credentials");

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
    }
}
