using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class RefreshToken : BaseEntity
    {
        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }

        // Foreign key
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;
    }
}
