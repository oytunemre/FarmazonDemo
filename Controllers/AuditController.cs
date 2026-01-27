using FarmazonDemo.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        /// <summary>
        /// Get audit logs for current user
        /// </summary>
        [HttpGet("my-logs")]
        public async Task<ActionResult> GetMyLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            var logs = await _auditService.GetUserAuditLogsAsync(userId, page, pageSize);
            return Ok(logs);
        }

        /// <summary>
        /// Get all audit logs (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> GetAllLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _auditService.GetAllAuditLogsAsync(page, pageSize);
            return Ok(logs);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user token");
            return userId;
        }
    }
}
