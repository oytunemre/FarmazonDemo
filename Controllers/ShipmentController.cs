using FarmazonDemo.Models.Dto.Shipment;
using FarmazonDemo.Services.Shipments;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize(Roles = "Admin,Seller")]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    // SellerOrder'a bağlı shipment getir
    [HttpGet("seller-orders/{sellerOrderId:int}")]
    public async Task<IActionResult> GetBySellerOrderId(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.GetBySellerOrderIdAsync(sellerOrderId, ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Ship
    [HttpPatch("seller-orders/{sellerOrderId:int}/ship")]
    public async Task<IActionResult> Ship(int sellerOrderId, [FromBody] ShipSellerOrderDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.ShipAsync(sellerOrderId, dto, ct));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Deliver
    [HttpPatch("seller-orders/{sellerOrderId:int}/deliver")]
    public async Task<IActionResult> Deliver(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.DeliverAsync(sellerOrderId, ct));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Timeline (Admin panel için altın endpoint)
    [HttpGet("seller-orders/{sellerOrderId:int}/timeline")]
    public async Task<IActionResult> Timeline(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.GetTimelineBySellerOrderIdAsync(sellerOrderId, ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
