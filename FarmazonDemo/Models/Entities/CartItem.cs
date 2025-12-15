using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class CartItem : BaseEntity
    {
        [Key]
        public int CartItemId { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int Quantity { get; set; }

        // Sepete eklenirken yakalanan fiyat (ürün fiyatı sonradan değişirse sepet bozulmasın)
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;

    }
}
