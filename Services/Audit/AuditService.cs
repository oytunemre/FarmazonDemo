using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, int? userId = null, string? entityType = null,
            int? entityId = null, string? oldValues = null, string? newValues = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                Action = action,
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogLoginAttemptAsync(int? userId, bool success, string? ipAddress = null)
        {
            var action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            await LogAsync(action, userId, "Users", userId);
        }

        public async Task LogPasswordChangeAsync(int userId)
        {
            await LogAsync("PASSWORD_CHANGED", userId, "Users", userId);
        }

        public async Task LogRoleChangeAsync(int userId, string oldRole, string newRole, int changedByUserId)
        {
            await LogAsync("ROLE_CHANGED", changedByUserId, "Users", userId, oldRole, newRole);
        }

        public async Task<IEnumerable<object>> GetUserAuditLogsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.IpAddress,
                    a.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetAllAuditLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    Username = a.User != null ? a.User.Username : null,
                    a.IpAddress,
                    a.CreatedAt
                })
                .ToListAsync();
        }

        private static string? GetClientIpAddress(HttpContext? context)
        {
            if (context == null) return null;

            // Check for forwarded IP (behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
