using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities
{
    public class Cart : BaseEntity
    {
        [Key]
        public int CartId { get; set; }

        // 1 kullanıcı -> 1 aktif sepet (basit versiyon)
        public int UserId { get; set; }
        public Users User { get; set; } = null!;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }


}
