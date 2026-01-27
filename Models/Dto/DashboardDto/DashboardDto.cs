using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.DashboardDto;

// Main Dashboard Response
public class AdminDashboardDto
{
    public DashboardOverviewDto Overview { get; set; } = new();
    public SalesStatisticsDto Sales { get; set; } = new();
    public OrderStatisticsDto Orders { get; set; } = new();
    public UserStatisticsDto Users { get; set; } = new();
    public ProductStatisticsDto Products { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

// Overview Statistics
public class DashboardOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueChange { get; set; } // Percentage change from previous period
    public int TotalOrders { get; set; }
    public int OrdersChange { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public decimal AverageOrderValue { get; set; }
    public string Currency { get; set; } = "TRY";
}

// Sales Statistics
public class SalesStatisticsDto
{
    public decimal TotalSales { get; set; }
    public decimal PreviousPeriodSales { get; set; }
    public decimal GrowthPercentage { get; set; }
    public List<SalesDataPointDto> DailySales { get; set; } = new();
    public List<SalesByCategoryDto> SalesByCategory { get; set; } = new();
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
}

public class SalesDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
}

public class SalesByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
}

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

// Order Statistics
public class OrderStatisticsDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int RefundedOrders { get; set; }
    public decimal FulfillmentRate { get; set; } // Percentage of completed orders
    public decimal CancellationRate { get; set; }
    public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new();
}

public class OrdersByStatusDto
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

// User Statistics
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; } // Logged in within last 30 days
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSellers { get; set; }
    public int TotalAdmins { get; set; }
    public int VerifiedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public List<UserGrowthDataDto> UserGrowth { get; set; } = new();
}

public class UserGrowthDataDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int TotalUsers { get; set; }
}

// Product Statistics
public class ProductStatisticsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; } // Stock < 10
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int TotalCategories { get; set; }
    public List<ProductsByCategoryDto> ProductsByCategory { get; set; } = new();
}

public class ProductsByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int ListingCount { get; set; }
}

// Recent Activity
public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty; // Order, User, Product, Payment
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
}

// Report DTOs
public class SalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalShipping { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<SalesDataPointDto> DailyBreakdown { get; set; } = new();
    public List<SalesByCategoryDto> CategoryBreakdown { get; set; } = new();
    public List<SalesBySellerDto> SellerBreakdown { get; set; } = new();
}

public class SalesBySellerDto
{
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public int ItemsSold { get; set; }
}

public class InventoryReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalProducts { get; set; }
    public int TotalListings { get; set; }
    public int InStockListings { get; set; }
    public int OutOfStockListings { get; set; }
    public int LowStockListings { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<InventoryItemDto> LowStockItems { get; set; } = new();
    public List<InventoryItemDto> OutOfStockItems { get; set; } = new();
}

public class InventoryItemDto
{
    public int ListingId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
}

public class CustomerReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    public decimal CustomerRetentionRate { get; set; }
    public decimal AverageOrdersPerCustomer { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class TopCustomerDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime LastOrderDate { get; set; }
}
