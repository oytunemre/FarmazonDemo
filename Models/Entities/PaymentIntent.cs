using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities;

public class PaymentIntent : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; } = 0;

    [StringLength(3)]
    public string Currency { get; set; } = "TRY";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    [StringLength(50)]
    public string Provider { get; set; } = "MANUAL";

    [StringLength(100)]
    public string? ExternalReference { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }

    // Card info (masked)
    [StringLength(20)]
    public string? CardLast4 { get; set; }

    [StringLength(20)]
    public string? CardBrand { get; set; } // Visa, Mastercard, etc.

    // Timestamps
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // 3D Secure
    public bool Requires3DSecure { get; set; } = false;

    [StringLength(500)]
    public string? ThreeDSecureUrl { get; set; }

    // Installment
    public int? InstallmentCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InstallmentAmount { get; set; }

    // Metadata (JSON)
    [StringLength(2000)]
    public string? Metadata { get; set; }

    // Navigation
    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}
