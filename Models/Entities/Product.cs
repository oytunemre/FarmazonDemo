using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities
{
    public class Product : BaseEntity
    {

        [Key]
        public int ProductId { get; set; }

        public required string ProductName { get; set; } = string.Empty;

        public required string ProductDescription { get; set; } = string.Empty;

        public required string ProductBarcode { get; set; } = string.Empty;

     



    }
}
