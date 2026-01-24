using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.Shipment;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Shipments;

public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _db;

    public ShipmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ShipmentDto> GetBySellerOrderIdAsync(int sellerOrderId, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, ct);

        if (shipment is null)
            throw new NotFoundException("Shipment not found for SellerOrder.");

        return Map(shipment);
    }

    public async Task<ShipmentDto> ShipAsync(int sellerOrderId, ShipSellerOrderDto dto, CancellationToken ct = default)
    {
        var sellerOrder = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId, ct);

        if (sellerOrder is null)
            throw new NotFoundException("SellerOrder not found.");

        sellerOrder.Shipment ??= new Shipment
        {
            SellerOrderId = sellerOrder.Id,
            Status = ShipmentStatus.Created
        };

        var shipment = sellerOrder.Shipment;

        if (shipment.Status == ShipmentStatus.Delivered)
            throw new BadRequestException("Delivered shipment tekrar ship edilemez.");

        shipment.Carrier = dto.Carrier.Trim();
        shipment.TrackingNumber = dto.TrackingNumber.Trim();

        if (shipment.Status is ShipmentStatus.Created or ShipmentStatus.Packed)
        {
            shipment.Status = ShipmentStatus.Shipped;
            shipment.ShippedAt ??= DateTime.UtcNow;
        }
        else if (shipment.Status == ShipmentStatus.Shipped)
        {
            // tracking update gibi
        }
        else
        {
            throw new BadRequestException($"Shipment bu durumdan ship edilemez: {shipment.Status}");
        }

        sellerOrder.Status = SellerOrderStatus.Shipped;

        await _db.SaveChangesAsync(ct);

        await AppendEventAsync(
            shipmentId: shipment.Id,
            status: shipment.Status,
            payloadJson: $"{{\"action\":\"ship\",\"carrier\":\"{Esc(shipment.Carrier)}\",\"tracking\":\"{Esc(shipment.TrackingNumber)}\"}}",
            ct: ct
        );

        return Map(shipment);
    }

    public async Task<ShipmentDto> DeliverAsync(int sellerOrderId, CancellationToken ct = default)
    {
        var sellerOrder = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId, ct);

        if (sellerOrder is null)
            throw new NotFoundException("SellerOrder not found.");

        if (sellerOrder.Shipment is null)
            throw new BadRequestException("Shipment yok. Önce ship yap.");

        var shipment = sellerOrder.Shipment;

        if (shipment.Status != ShipmentStatus.Shipped)
            throw new BadRequestException("Deliver için shipment önce Shipped olmalı.");

        shipment.Status = ShipmentStatus.Delivered;
        shipment.DeliveredAt = DateTime.UtcNow;

        sellerOrder.Status = SellerOrderStatus.Delivered;

        await _db.SaveChangesAsync(ct);

        await AppendEventAsync(
            shipmentId: shipment.Id,
            status: shipment.Status,
            payloadJson: $"{{\"action\":\"deliver\"}}",
            ct: ct
        );

        return Map(shipment);
    }

    public async Task<List<ShipmentEventDto>> GetTimelineBySellerOrderIdAsync(int sellerOrderId, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, ct);

        if (shipment is null)
            throw new NotFoundException("Shipment not found for SellerOrder.");

        var events = await _db.ShipmentEvents
            .Where(e => e.ShipmentId == shipment.Id)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        return events.Select(e => new ShipmentEventDto
        {
            Id = e.ShipmentEventId,
            ShipmentId = e.ShipmentId,
            Status = e.Status,
            PayloadJson = e.PayloadJson,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    private async Task AppendEventAsync(int shipmentId, ShipmentStatus status, string? payloadJson, CancellationToken ct)
    {
        _db.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = shipmentId,
            Status = status,
            PayloadJson = payloadJson
        });

        await _db.SaveChangesAsync(ct);
    }

    private static ShipmentDto Map(Shipment x) => new()
    {
        Id = x.Id,
        SellerOrderId = x.SellerOrderId,
        Carrier = x.Carrier,
        TrackingNumber = x.TrackingNumber,
        Status = x.Status,
        ShippedAt = x.ShippedAt,
        DeliveredAt = x.DeliveredAt
    };

    private static string Esc(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
