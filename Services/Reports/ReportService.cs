using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.DashboardDto;
using FarmazonDemo.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Reports;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SalesReportDto> GenerateSalesReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate &&
                        o.Status == OrderStatus.Completed && !o.IsDeleted)
            .Include(o => o.SellerOrders)
                .ThenInclude(so => so.Items)
                    .ThenInclude(oi => oi.Listing)
                        .ThenInclude(l => l.Product)
                            .ThenInclude(p => p.Category)
            .ToListAsync(ct);

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalTax = orders.Sum(o => o.TaxAmount);
        var totalShipping = orders.Sum(o => o.ShippingCost);
        var totalDiscounts = orders.Sum(o => o.DiscountAmount);
        var totalItemsSold = orders.SelectMany(o => o.SellerOrders).SelectMany(so => so.Items).Sum(i => i.Quantity);

        // Daily breakdown
        var dailyBreakdown = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new SalesDataPointDto
            {
                Date = g.Key,
                Amount = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Category breakdown
        var categoryBreakdown = orders
            .SelectMany(o => o.SellerOrders)
            .SelectMany(so => so.Items)
            .Where(i => i.Listing?.Product?.Category != null)
            .GroupBy(i => new { i.Listing.Product.CategoryId, i.Listing.Product.Category!.Name })
            .Select(g => new SalesByCategoryDto
            {
                CategoryId = g.Key.CategoryId ?? 0,
                CategoryName = g.Key.Name,
                TotalSales = g.Sum(i => i.TotalPrice),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.TotalSales)
            .ToList();

        // Calculate percentages
        if (categoryBreakdown.Any())
        {
            var total = categoryBreakdown.Sum(c => c.TotalSales);
            foreach (var category in categoryBreakdown)
            {
                category.Percentage = total > 0 ? (category.TotalSales / total) * 100 : 0;
            }
        }

        // Seller breakdown
        var sellerBreakdown = await _db.SellerOrders
            .Where(so => so.CreatedAt >= fromDate && so.CreatedAt <= toDate && !so.IsDeleted)
            .Include(so => so.Seller)
            .Include(so => so.Items)
            .GroupBy(so => new { so.SellerId, so.Seller.FirstName, so.Seller.LastName })
            .Select(g => new SalesBySellerDto
            {
                SellerId = g.Key.SellerId,
                SellerName = g.Key.FirstName + " " + g.Key.LastName,
                TotalSales = g.Sum(so => so.SubTotal),
                OrderCount = g.Count(),
                ItemsSold = g.SelectMany(so => so.Items).Sum(i => i.Quantity)
            })
            .OrderByDescending(s => s.TotalSales)
            .ToListAsync(ct);

        return new SalesReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            TotalTax = totalTax,
            TotalShipping = totalShipping,
            TotalDiscounts = totalDiscounts,
            NetRevenue = totalRevenue - totalTax - totalShipping + totalDiscounts,
            TotalOrders = orders.Count,
            TotalItemsSold = totalItemsSold,
            AverageOrderValue = orders.Any() ? totalRevenue / orders.Count : 0,
            DailyBreakdown = dailyBreakdown,
            CategoryBreakdown = categoryBreakdown,
            SellerBreakdown = sellerBreakdown
        };
    }

    public async Task<InventoryReportDto> GenerateInventoryReportAsync(CancellationToken ct = default)
    {
        var listings = await _db.Listings
            .Where(l => !l.IsDeleted)
            .Include(l => l.Product)
            .Include(l => l.Seller)
            .ToListAsync(ct);

        var lowStockItems = listings
            .Where(l => l.StockQuantity > 0 && l.StockQuantity < 10)
            .Select(l => new InventoryItemDto
            {
                ListingId = l.Id,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? "Unknown",
                SellerName = $"{l.Seller?.FirstName} {l.Seller?.LastName}",
                CurrentStock = l.StockQuantity,
                Price = l.Price,
                Sku = l.Sku
            })
            .OrderBy(i => i.CurrentStock)
            .ToList();

        var outOfStockItems = listings
            .Where(l => l.StockQuantity == 0)
            .Select(l => new InventoryItemDto
            {
                ListingId = l.Id,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? "Unknown",
                SellerName = $"{l.Seller?.FirstName} {l.Seller?.LastName}",
                CurrentStock = 0,
                Price = l.Price,
                Sku = l.Sku
            })
            .ToList();

        return new InventoryReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            TotalProducts = await _db.Products.CountAsync(p => !p.IsDeleted, ct),
            TotalListings = listings.Count,
            InStockListings = listings.Count(l => l.StockQuantity > 0),
            OutOfStockListings = outOfStockItems.Count,
            LowStockListings = lowStockItems.Count,
            TotalInventoryValue = listings.Sum(l => l.Price * l.StockQuantity),
            LowStockItems = lowStockItems,
            OutOfStockItems = outOfStockItems
        };
    }

    public async Task<CustomerReportDto> GenerateCustomerReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var customers = await _db.Users
            .Where(u => u.Role == "Customer" && !u.IsDeleted)
            .ToListAsync(ct);

        var newCustomers = customers.Count(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate);

        // Customers with orders in period
        var customerOrders = await _db.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
            .GroupBy(o => o.BuyerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
                LastOrderDate = g.Max(o => o.CreatedAt)
            })
            .ToListAsync(ct);

        var returningCustomers = customerOrders.Count(c => c.OrderCount > 1);

        // Top customers
        var topCustomers = await _db.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && !o.IsDeleted)
            .Include(o => o.Buyer)
            .GroupBy(o => new { o.BuyerId, o.Buyer.FirstName, o.Buyer.LastName, o.Buyer.Email })
            .Select(g => new TopCustomerDto
            {
                UserId = g.Key.BuyerId,
                Name = g.Key.FirstName + " " + g.Key.LastName,
                Email = g.Key.Email,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
                LastOrderDate = g.Max(o => o.CreatedAt)
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(20)
            .ToListAsync(ct);

        var totalOrders = customerOrders.Sum(c => c.OrderCount);

        return new CustomerReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalCustomers = customers.Count,
            NewCustomers = newCustomers,
            ReturningCustomers = returningCustomers,
            CustomerRetentionRate = customers.Count > 0 ? (decimal)returningCustomers / customers.Count * 100 : 0,
            AverageOrdersPerCustomer = customerOrders.Any() ? (decimal)totalOrders / customerOrders.Count : 0,
            AverageLifetimeValue = customers.Count > 0 ? customerOrders.Sum(c => c.TotalSpent) / customers.Count : 0,
            TopCustomers = topCustomers
        };
    }
}
