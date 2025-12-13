namespace FarmazonDemo.Models.Dto.ListingDto
{
    public class CreateListingDto
    {
        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Condition { get; set; } = "New";
    }
}
