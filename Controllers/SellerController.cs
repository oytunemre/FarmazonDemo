using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.SellerDto;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/seller")]
[Authorize(Roles = "Admin,Seller")]
public class SellerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SellerController(ApplicationDbContext db) => _db = db;

    // 1) Satıcı siparişleri
    [HttpGet("{sellerId:int}/orders")]
    public async Task<IActionResult> GetSellerOrders(int sellerId)
    {
        var list = await _db.SellerOrders
            .Where(x => x.SellerId == sellerId)
            .Include(x => x.Items)
            .Include(x => x.Shipment)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var dto = list.Select(so => new SellerOrderListDto
        {
            SellerOrderId = so.Id,
            OrderId = so.OrderId,
            SellerId = so.SellerId,
            Status = so.Status,
            SubTotal = so.SubTotal,
            Items = so.Items.Select(i => new SellerOrderItemDto
            {
                SellerOrderItemId = i.Id,
                ListingId = i.ListingId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList(),
            Shipment = so.Shipment == null ? null : new SellerShipmentDto
            {
                Carrier = so.Shipment.Carrier,
                TrackingNumber = so.Shipment.TrackingNumber,
                Status = so.Shipment.Status,
                ShippedAt = so.Shipment.ShippedAt,
                DeliveredAt = so.Shipment.DeliveredAt
            }
        }).ToList();

        return Ok(dto);
    }

    // 2) Kargoya ver (shipment güncelle)
    [HttpPatch("orders/{sellerOrderId:int}/ship")]
    public async Task<IActionResult> Ship(int sellerOrderId, [FromBody] ShipSellerOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Carrier) || string.IsNullOrWhiteSpace(dto.TrackingNumber))
            throw new BadRequestException("Carrier ve TrackingNumber zorunlu.");

        var so = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId);

        if (so is null) throw new NotFoundException("SellerOrder not found.");

        // Eğer shipment yoksa oluştur
        so.Shipment ??= new Models.Entities.Shipment { SellerOrderId = so.Id };

        // Status kontrol
        if (so.Shipment.Status is ShipmentStatus.Delivered)
            throw new BadRequestException("Teslim edilmiş shipment tekrar ship edilemez.");

        so.Shipment.Carrier = dto.Carrier.Trim();
        so.Shipment.TrackingNumber = dto.TrackingNumber.Trim();
        so.Shipment.Status = ShipmentStatus.Shipped;
        so.Shipment.ShippedAt ??= DateTime.UtcNow;

        so.Status = SellerOrderStatus.Shipped;

        await _db.SaveChangesAsync();
        return Ok();
    }

    // 3) Teslim edildi
    [HttpPatch("orders/{sellerOrderId:int}/deliver")]
    public async Task<IActionResult> Deliver(int sellerOrderId)
    {
        var so = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId);

        if (so is null) throw new NotFoundException("SellerOrder not found.");
        if (so.Shipment is null) throw new BadRequestException("Shipment yok. Önce ship yap.");

        if (so.Shipment.Status != ShipmentStatus.Shipped)
            throw new BadRequestException("Deliver için shipment önce Shipped olmalı.");

        so.Shipment.Status = ShipmentStatus.Delivered;
        so.Shipment.DeliveredAt = DateTime.UtcNow;

        so.Status = SellerOrderStatus.Delivered;

        await _db.SaveChangesAsync();
        return Ok();
    }
}
