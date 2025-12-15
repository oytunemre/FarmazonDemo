namespace FarmazonDemo.Models.Dto.CartDto
{
    public class AddToCartDto
    {
        public int UserId { get; set; }
        public int ListingId { get; set; }
        public int Quantity { get; set; }
    }
}
