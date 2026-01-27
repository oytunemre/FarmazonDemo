using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Services.TwoFactor;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TwoFactorController : ControllerBase
    {
        private readonly ITwoFactorService _twoFactorService;
        private readonly ApplicationDbContext _context;

        public TwoFactorController(ITwoFactorService twoFactorService, ApplicationDbContext context)
        {
            _twoFactorService = twoFactorService;
            _context = context;
        }

        /// <summary>
        /// Get 2FA status for current user
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<TwoFactorStatusDto>> GetStatus()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            return Ok(new TwoFactorStatusDto
            {
                IsEnabled = user.TwoFactorEnabled,
                EnabledAt = user.TwoFactorEnabledAt
            });
        }

        /// <summary>
        /// Setup 2FA - generates secret key and QR code URI
        /// </summary>
        [HttpPost("setup")]
        [EnableRateLimiting("sensitive")]
        public async Task<ActionResult<TwoFactorSetupResponseDto>> Setup()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (user.TwoFactorEnabled)
                throw new BadRequestException("2FA is already enabled");

            // Generate secret key
            var secretKey = _twoFactorService.GenerateSecretKey();
            var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email, secretKey);
            var backupCodes = _twoFactorService.GenerateBackupCodes();

            // Store temporarily (not enabled yet, user must verify first)
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorBackupCodes = backupCodes;
            await _context.SaveChangesAsync();

            return Ok(new TwoFactorSetupResponseDto
            {
                SecretKey = secretKey,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes.Split(',')
            });
        }

        /// <summary>
        /// Verify and enable 2FA
        /// </summary>
        [HttpPost("verify")]
        [EnableRateLimiting("sensitive")]
        public async Task<ActionResult> Verify([FromBody] TwoFactorVerifyDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (user.TwoFactorEnabled)
                throw new BadRequestException("2FA is already enabled");

            if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
                throw new BadRequestException("Please setup 2FA first");

            // Verify the code
            var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecretKey, dto.Code);

            if (!isValid)
                throw new BadRequestException("Invalid verification code");

            // Enable 2FA
            user.TwoFactorEnabled = true;
            user.TwoFactorEnabledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Two-factor authentication enabled successfully" });
        }

        /// <summary>
        /// Disable 2FA
        /// </summary>
        [HttpPost("disable")]
        [EnableRateLimiting("sensitive")]
        public async Task<ActionResult> Disable([FromBody] TwoFactorVerifyDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (!user.TwoFactorEnabled)
                throw new BadRequestException("2FA is not enabled");

            // Verify the code before disabling
            var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecretKey!, dto.Code);

            if (!isValid)
            {
                // Check backup codes
                var backupCodes = user.TwoFactorBackupCodes?.Split(',').ToList() ?? new List<string>();
                if (!backupCodes.Contains(dto.Code))
                    throw new BadRequestException("Invalid verification code");

                // Remove used backup code
                backupCodes.Remove(dto.Code);
                user.TwoFactorBackupCodes = string.Join(",", backupCodes);
            }

            // Disable 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.TwoFactorBackupCodes = null;
            user.TwoFactorEnabledAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Two-factor authentication disabled successfully" });
        }

        /// <summary>
        /// Regenerate backup codes
        /// </summary>
        [HttpPost("backup-codes/regenerate")]
        [EnableRateLimiting("sensitive")]
        public async Task<ActionResult<string[]>> RegenerateBackupCodes([FromBody] TwoFactorVerifyDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (!user.TwoFactorEnabled)
                throw new BadRequestException("2FA is not enabled");

            // Verify the code
            var isValid = _twoFactorService.ValidateCode(user.TwoFactorSecretKey!, dto.Code);

            if (!isValid)
                throw new BadRequestException("Invalid verification code");

            // Generate new backup codes
            var backupCodes = _twoFactorService.GenerateBackupCodes();
            user.TwoFactorBackupCodes = backupCodes;
            await _context.SaveChangesAsync();

            return Ok(new { BackupCodes = backupCodes.Split(',') });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }
}
