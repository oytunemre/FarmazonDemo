using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.Payment;

public class PaymentIntentResponseDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }

    public string Provider { get; set; } = "MANUAL";
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }

    // Card info (masked)
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }

    // 3D Secure
    public bool Requires3DSecure { get; set; }
    public string? ThreeDSecureUrl { get; set; }

    // Installment
    public int? InstallmentCount { get; set; }
    public decimal? InstallmentAmount { get; set; }

    // Timestamps
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentEventDto
{
    public int Id { get; set; }
    public int PaymentIntentId { get; set; }
    public PaymentStatus Status { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RefundPaymentResponseDto
{
    public int PaymentIntentId { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime RefundedAt { get; set; }
}
