using FarmazonDemo.Models.Dto.DashboardDto;
using FarmazonDemo.Services.Dashboard;
using FarmazonDemo.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IReportService _reportService;

    public AdminDashboardController(IDashboardService dashboardService, IReportService reportService)
    {
        _dashboardService = dashboardService;
        _reportService = reportService;
    }

    /// <summary>
    /// Get complete admin dashboard data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync(fromDate, toDate, ct);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get dashboard overview statistics
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var overview = await _dashboardService.GetOverviewAsync(fromDate, toDate, ct);
        return Ok(overview);
    }

    /// <summary>
    /// Get sales statistics
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesStatisticsDto>> GetSalesStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var sales = await _dashboardService.GetSalesStatisticsAsync(from, to, ct);
        return Ok(sales);
    }

    /// <summary>
    /// Get order statistics
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<OrderStatisticsDto>> GetOrderStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var orders = await _dashboardService.GetOrderStatisticsAsync(fromDate, toDate, ct);
        return Ok(orders);
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics(CancellationToken ct = default)
    {
        var users = await _dashboardService.GetUserStatisticsAsync(ct);
        return Ok(users);
    }

    /// <summary>
    /// Get product statistics
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<ProductStatisticsDto>> GetProductStatistics(CancellationToken ct = default)
    {
        var products = await _dashboardService.GetProductStatisticsAsync(ct);
        return Ok(products);
    }

    /// <summary>
    /// Get recent activities
    /// </summary>
    [HttpGet("activities")]
    public async Task<ActionResult<List<RecentActivityDto>>> GetRecentActivities(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        var activities = await _dashboardService.GetRecentActivitiesAsync(count, ct);
        return Ok(activities);
    }

    /// <summary>
    /// Generate sales report
    /// </summary>
    [HttpGet("reports/sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var report = await _reportService.GenerateSalesReportAsync(from, to, ct);
        return Ok(report);
    }

    /// <summary>
    /// Generate inventory report
    /// </summary>
    [HttpGet("reports/inventory")]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport(CancellationToken ct = default)
    {
        var report = await _reportService.GenerateInventoryReportAsync(ct);
        return Ok(report);
    }

    /// <summary>
    /// Generate customer report
    /// </summary>
    [HttpGet("reports/customers")]
    public async Task<ActionResult<CustomerReportDto>> GetCustomerReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var report = await _reportService.GenerateCustomerReportAsync(from, to, ct);
        return Ok(report);
    }
}
