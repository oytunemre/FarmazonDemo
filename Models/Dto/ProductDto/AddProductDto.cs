namespace FarmazonDemo.Models.Dto.ProductDto
{
    public class AddProductDto
    {


        public string ProductName { get; set; }

        public string ProductDescription { get; set; }

        public required string ProductBarcode { get; set; }


    }
}
