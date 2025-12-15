namespace FarmazonDemo.Models.Dto.ListingDto
{
    public class UpdateListingDto
    {
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Condition { get; set; } = "New";
        public bool IsActive { get; set; }
    }
}
