using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.DashboardDto;
using FarmazonDemo.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        return new AdminDashboardDto
        {
            Overview = await GetOverviewAsync(from, to, ct),
            Sales = await GetSalesStatisticsAsync(from, to, ct),
            Orders = await GetOrderStatisticsAsync(from, to, ct),
            Users = await GetUserStatisticsAsync(ct),
            Products = await GetProductStatisticsAsync(ct),
            RecentActivities = await GetRecentActivitiesAsync(10, ct)
        };
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var previousFrom = from.AddDays(-(to - from).TotalDays);

        // Current period orders
        var currentOrders = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && !o.IsDeleted)
            .ToListAsync(ct);

        // Previous period orders
        var previousOrders = await _db.Orders
            .Where(o => o.CreatedAt >= previousFrom && o.CreatedAt < from && !o.IsDeleted)
            .ToListAsync(ct);

        var currentRevenue = currentOrders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
        var previousRevenue = previousOrders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);

        var totalCustomers = await _db.Users.CountAsync(u => u.Role == "Customer" && !u.IsDeleted, ct);
        var newCustomers = await _db.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= from && !u.IsDeleted, ct);

        var totalProducts = await _db.Products.CountAsync(p => !p.IsDeleted, ct);
        var lowStockProducts = await _db.Listings.CountAsync(l => l.StockQuantity > 0 && l.StockQuantity < 10 && !l.IsDeleted, ct);

        return new DashboardOverviewDto
        {
            TotalRevenue = currentRevenue,
            RevenueChange = previousRevenue > 0 ? ((currentRevenue - previousRevenue) / previousRevenue) * 100 : 0,
            TotalOrders = currentOrders.Count,
            OrdersChange = previousOrders.Count > 0 ? ((currentOrders.Count - previousOrders.Count) * 100) / previousOrders.Count : 0,
            TotalCustomers = totalCustomers,
            NewCustomers = newCustomers,
            TotalProducts = totalProducts,
            LowStockProducts = lowStockProducts,
            AverageOrderValue = currentOrders.Any() ? currentOrders.Average(o => o.TotalAmount) : 0,
            Currency = "TRY"
        };
    }

    public async Task<SalesStatisticsDto> GetSalesStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var previousFrom = fromDate.AddDays(-(toDate - fromDate).TotalDays);

        var currentOrders = await _db.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == OrderStatus.Completed && !o.IsDeleted)
            .ToListAsync(ct);

        var previousOrders = await _db.Orders
            .Where(o => o.CreatedAt >= previousFrom && o.CreatedAt < fromDate && o.Status == OrderStatus.Completed && !o.IsDeleted)
            .ToListAsync(ct);

        var totalSales = currentOrders.Sum(o => o.TotalAmount);
        var previousSales = previousOrders.Sum(o => o.TotalAmount);

        // Daily sales
        var dailySales = currentOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new SalesDataPointDto
            {
                Date = g.Key,
                Amount = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Top selling products
        var topProducts = await _db.OrderItems
            .Where(oi => oi.CreatedAt >= fromDate && oi.CreatedAt <= toDate && !oi.IsDeleted)
            .Include(oi => oi.Listing)
                .ThenInclude(l => l.Product)
            .GroupBy(oi => new { oi.Listing.ProductId, oi.Listing.Product.Name })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.TotalPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync(ct);

        // Sales by category
        var salesByCategory = await _db.OrderItems
            .Where(oi => oi.CreatedAt >= fromDate && oi.CreatedAt <= toDate && !oi.IsDeleted)
            .Include(oi => oi.Listing)
                .ThenInclude(l => l.Product)
                    .ThenInclude(p => p.Category)
            .Where(oi => oi.Listing.Product.Category != null)
            .GroupBy(oi => new { oi.Listing.Product.CategoryId, oi.Listing.Product.Category!.Name })
            .Select(g => new SalesByCategoryDto
            {
                CategoryId = g.Key.CategoryId ?? 0,
                CategoryName = g.Key.Name,
                TotalSales = g.Sum(oi => oi.TotalPrice),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.TotalSales)
            .ToListAsync(ct);

        // Calculate percentages
        if (salesByCategory.Any())
        {
            var total = salesByCategory.Sum(c => c.TotalSales);
            foreach (var category in salesByCategory)
            {
                category.Percentage = total > 0 ? (category.TotalSales / total) * 100 : 0;
            }
        }

        return new SalesStatisticsDto
        {
            TotalSales = totalSales,
            PreviousPeriodSales = previousSales,
            GrowthPercentage = previousSales > 0 ? ((totalSales - previousSales) / previousSales) * 100 : 0,
            DailySales = dailySales,
            TopSellingProducts = topProducts,
            SalesByCategory = salesByCategory
        };
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && !o.IsDeleted)
            .ToListAsync(ct);

        var totalOrders = orders.Count;
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
        var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

        var ordersByStatus = Enum.GetValues<OrderStatus>()
            .Select(status => new OrdersByStatusDto
            {
                Status = status,
                Count = orders.Count(o => o.Status == status),
                Percentage = totalOrders > 0 ? (decimal)orders.Count(o => o.Status == status) / totalOrders * 100 : 0
            })
            .Where(o => o.Count > 0)
            .ToList();

        return new OrderStatisticsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
            ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped),
            DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            RefundedOrders = orders.Count(o => o.Status == OrderStatus.Refunded),
            FulfillmentRate = totalOrders > 0 ? (decimal)completedOrders / totalOrders * 100 : 0,
            CancellationRate = totalOrders > 0 ? (decimal)cancelledOrders / totalOrders * 100 : 0,
            OrdersByStatus = ordersByStatus
        };
    }

    public async Task<UserStatisticsDto> GetUserStatisticsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var users = await _db.Users.Where(u => !u.IsDeleted).ToListAsync(ct);

        // User growth data (last 30 days)
        var userGrowth = users
            .Where(u => u.CreatedAt >= monthAgo)
            .GroupBy(u => u.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new UserGrowthDataDto
            {
                Date = g.Key,
                NewUsers = g.Count(),
                TotalUsers = users.Count(u => u.CreatedAt <= g.Key)
            })
            .ToList();

        return new UserStatisticsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.LastLoginAt >= monthAgo),
            NewUsersToday = users.Count(u => u.CreatedAt >= today),
            NewUsersThisWeek = users.Count(u => u.CreatedAt >= weekAgo),
            NewUsersThisMonth = users.Count(u => u.CreatedAt >= monthAgo),
            TotalCustomers = users.Count(u => u.Role == "Customer"),
            TotalSellers = users.Count(u => u.Role == "Seller"),
            TotalAdmins = users.Count(u => u.Role == "Admin"),
            VerifiedUsers = users.Count(u => u.EmailConfirmed),
            UnverifiedUsers = users.Count(u => !u.EmailConfirmed),
            UserGrowth = userGrowth
        };
    }

    public async Task<ProductStatisticsDto> GetProductStatisticsAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.Where(p => !p.IsDeleted).ToListAsync(ct);
        var listings = await _db.Listings.Where(l => !l.IsDeleted).ToListAsync(ct);
        var categories = await _db.Categories.Where(c => !c.IsDeleted).ToListAsync(ct);

        var productsByCategory = await _db.Products
            .Where(p => !p.IsDeleted && p.CategoryId != null)
            .Include(p => p.Category)
            .GroupBy(p => new { p.CategoryId, p.Category!.Name })
            .Select(g => new ProductsByCategoryDto
            {
                CategoryId = g.Key.CategoryId ?? 0,
                CategoryName = g.Key.Name,
                ProductCount = g.Count(),
                ListingCount = g.SelectMany(p => p.Listings).Count(l => !l.IsDeleted)
            })
            .ToListAsync(ct);

        return new ProductStatisticsDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            InactiveProducts = products.Count(p => !p.IsActive),
            OutOfStockProducts = listings.Count(l => l.StockQuantity == 0),
            LowStockProducts = listings.Count(l => l.StockQuantity > 0 && l.StockQuantity < 10),
            TotalListings = listings.Count,
            ActiveListings = listings.Count(l => l.IsActive),
            TotalCategories = categories.Count,
            ProductsByCategory = productsByCategory
        };
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10, CancellationToken ct = default)
    {
        var activities = new List<RecentActivityDto>();

        // Recent orders
        var recentOrders = await _db.Orders
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Include(o => o.Buyer)
            .ToListAsync(ct);

        activities.AddRange(recentOrders.Select(o => new RecentActivityDto
        {
            Type = "Order",
            Title = $"New Order #{o.OrderNumber}",
            Description = $"{o.Buyer?.FirstName} {o.Buyer?.LastName} placed an order for {o.TotalAmount:N2} TRY",
            Timestamp = o.CreatedAt,
            EntityId = o.Id.ToString(),
            EntityType = "Order"
        }));

        // Recent users
        var recentUsers = await _db.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        activities.AddRange(recentUsers.Select(u => new RecentActivityDto
        {
            Type = "User",
            Title = $"New {u.Role} Registration",
            Description = $"{u.FirstName} {u.LastName} ({u.Email}) registered",
            Timestamp = u.CreatedAt,
            EntityId = u.Id.ToString(),
            EntityType = "User"
        }));

        // Recent payments
        var recentPayments = await _db.PaymentIntents
            .Where(p => p.Status == PaymentStatus.Captured && !p.IsDeleted)
            .OrderByDescending(p => p.CapturedAt)
            .Take(5)
            .Include(p => p.Order)
            .ToListAsync(ct);

        activities.AddRange(recentPayments.Select(p => new RecentActivityDto
        {
            Type = "Payment",
            Title = "Payment Received",
            Description = $"Payment of {p.Amount:N2} {p.Currency} captured for order #{p.Order?.OrderNumber}",
            Timestamp = p.CapturedAt ?? p.CreatedAt,
            EntityId = p.Id.ToString(),
            EntityType = "Payment"
        }));

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }
}
