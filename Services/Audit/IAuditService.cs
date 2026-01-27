namespace FarmazonDemo.Services.Audit
{
    public interface IAuditService
    {
        Task LogAsync(string action, int? userId = null, string? entityType = null, int? entityId = null,
            string? oldValues = null, string? newValues = null);
        Task LogLoginAttemptAsync(int? userId, bool success, string? ipAddress = null);
        Task LogPasswordChangeAsync(int userId);
        Task LogRoleChangeAsync(int userId, string oldRole, string newRole, int changedByUserId);
        Task<IEnumerable<object>> GetUserAuditLogsAsync(int userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<object>> GetAllAuditLogsAsync(int page = 1, int pageSize = 50);
    }
}
