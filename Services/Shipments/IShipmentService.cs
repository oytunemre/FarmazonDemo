using FarmazonDemo.Models.Dto.Shipment;

namespace FarmazonDemo.Services.Shipments;

public interface IShipmentService
{
    Task<ShipmentDto> GetBySellerOrderIdAsync(int sellerOrderId, CancellationToken ct = default);
    Task<ShipmentDto> ShipAsync(int sellerOrderId, ShipSellerOrderDto dto, CancellationToken ct = default);
    Task<ShipmentDto> DeliverAsync(int sellerOrderId, CancellationToken ct = default);
    Task<List<ShipmentEventDto>> GetTimelineBySellerOrderIdAsync(int sellerOrderId, CancellationToken ct = default);
}
