namespace FarmazonDemo.Models.Dto.CartDto;

public class CartResponseDto
{
    public int CartId { get; set; }
    public int UserId { get; set; }
    public decimal CartTotal { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = new();
}

public class CartItemResponseDto
{
    public int CartItemId { get; set; }
    public int ListingId { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
