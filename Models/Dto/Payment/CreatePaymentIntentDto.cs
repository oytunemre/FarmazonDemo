using System.ComponentModel.DataAnnotations;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.Payment;

public class CreatePaymentIntentDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "TRY";

    // Optional card details for card payments
    public CardDetailsDto? CardDetails { get; set; }

    // Installment options
    public int? InstallmentCount { get; set; }

    // Return URL for 3D Secure
    [StringLength(500)]
    public string? ReturnUrl { get; set; }

    // Metadata
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CardDetailsDto
{
    [Required]
    [StringLength(19)]
    public required string CardNumber { get; set; }

    [Required]
    [StringLength(5)]
    public required string ExpiryDate { get; set; } // MM/YY

    [Required]
    [StringLength(4)]
    public required string Cvv { get; set; }

    [Required]
    [StringLength(100)]
    public required string CardHolderName { get; set; }
}

public class RefundPaymentDto
{
    public decimal? Amount { get; set; } // null = full refund

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class ProcessWebhookDto
{
    [Required]
    public required string Payload { get; set; }

    public string? Signature { get; set; }
}
