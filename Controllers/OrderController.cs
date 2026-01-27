using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Services.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutOrderDto dto)
        {
            var created = await _service.CheckoutAsync(dto.UserId);
            return CreatedAtAction(nameof(GetById), new { orderId = created.OrderId }, created);
        }

        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetById(int orderId)
            => Ok(await _service.GetByIdAsync(orderId));

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
            => Ok(await _service.GetByUserAsync(userId));
    }
}
