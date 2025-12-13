using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class Listing : BaseEntity
    {
        [Key]
        public int ListingId { get; set; }

        // FK -> Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // FK -> Users (Seller)
        public int SellerId { get; set; }
        public Users Seller { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        [MaxLength(30)]
        public string Condition { get; set; } = "New";

        public bool IsActive { get; set; } = true;

    }
}
