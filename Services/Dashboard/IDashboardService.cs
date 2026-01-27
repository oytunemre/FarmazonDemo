using FarmazonDemo.Models.Dto.DashboardDto;

namespace FarmazonDemo.Services.Dashboard;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<DashboardOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<SalesStatisticsDto> GetSalesStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<UserStatisticsDto> GetUserStatisticsAsync(CancellationToken ct = default);
    Task<ProductStatisticsDto> GetProductStatisticsAsync(CancellationToken ct = default);
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10, CancellationToken ct = default);
}
