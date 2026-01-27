using FarmazonDemo.Models.Dto;
using FarmazonDemo.Services.Auth;
using FarmazonDemo.Services.TwoFactor;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ApplicationDbContext _context;

        public AuthController(
            IAuthService authService,
            ITwoFactorService twoFactorService,
            ApplicationDbContext context)
        {
            _authService = authService;
            _twoFactorService = twoFactorService;
            _context = context;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Login with email/username and password
        /// Returns TwoFactorRequired: true if 2FA is enabled
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Login with 2FA code (when 2FA is enabled)
        /// </summary>
        [HttpPost("login/2fa")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> LoginWith2FA([FromBody] TwoFactorLoginDto dto)
        {
            var result = await _authService.LoginWith2FAAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Get current user info (requires authentication)
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public ActionResult<object> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                UserId = userId,
                Username = username,
                Email = email,
                Role = role
            });
        }

        /// <summary>
        /// Admin only endpoint - Get all users summary
        /// </summary>
        [HttpGet("admin/stats")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<object>> GetAdminStats()
        {
            var stats = await _authService.GetUserStatsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Seller or Admin endpoint
        /// </summary>
        [HttpGet("seller/dashboard")]
        [Authorize(Policy = "SellerOnly")]
        public ActionResult<object> GetSellerDashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Ok(new { Message = "Seller Dashboard", UserId = userId });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }

        /// <summary>
        /// Revoke refresh token (logout)
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequestDto dto)
        {
            await _authService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return Ok(new { Message = "Token revoked successfully" });
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<ActionResult> VerifyEmail([FromQuery] string token)
        {
            await _authService.VerifyEmailAsync(token);
            return Ok(new { Message = "Email verified successfully. You can now login." });
        }

        /// <summary>
        /// Resend verification email
        /// </summary>
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<ActionResult> ResendVerificationEmail([FromBody] EmailRequestDto dto)
        {
            await _authService.ResendVerificationEmailAsync(dto.Email);
            return Ok(new { Message = "Verification email sent. Please check your inbox." });
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { Message = "If the email exists, a password reset link has been sent." });
        }

        /// <summary>
        /// Reset password with token
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
            return Ok(new { Message = "Password reset successfully. You can now login with your new password." });
        }
    }
}
