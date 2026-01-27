using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("intents")]
    public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _paymentService.CreateIntentAsync(dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetByOrderId([FromRoute] int orderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _paymentService.GetByOrderIdAsync(orderId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{paymentIntentId:int}/mark-received")]
    public async Task<IActionResult> MarkReceived([FromRoute] int paymentIntentId, [FromBody] MarkPaymentReceivedDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _paymentService.MarkReceivedAsync(paymentIntentId, dto.Note, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{paymentIntentId:int}/simulate-success")]
    public async Task<IActionResult> SimulateSuccess([FromRoute] int paymentIntentId, [FromBody] SimulatePaymentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _paymentService.SimulateSuccessAsync(paymentIntentId, dto.Reason, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{paymentIntentId:int}/simulate-fail")]
    public async Task<IActionResult> SimulateFail([FromRoute] int paymentIntentId, [FromBody] SimulatePaymentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _paymentService.SimulateFailAsync(paymentIntentId, dto.Reason, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
