using FarmazonDemo.Models.Dto.CartDto;
using FarmazonDemo.Services.Carts;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
    {
        var cart = await _cartService.AddToCartAsync(dto);
        return Ok(cart);
    }

    [HttpPut("item/{cartItemId:int}")]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        var cart = await _cartService.UpdateCartItemQuantityAsync(cartItemId, dto.Quantity);
        return Ok(cart);
    }

    [HttpDelete("item/{cartItemId:int}")]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        var cart = await _cartService.RemoveItemAsync(cartItemId);
        return Ok(cart);
    }
}
