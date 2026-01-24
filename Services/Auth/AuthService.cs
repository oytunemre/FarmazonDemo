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
using System.Text;
using BCrypt.Net;

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
            var user = new Users
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

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt
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

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt
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
    }
}
