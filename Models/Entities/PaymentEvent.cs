using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities;

public class PaymentEvent : BaseEntity
{
    public int PaymentIntentId { get; set; }

    [ForeignKey("PaymentIntentId")]
    public PaymentIntent PaymentIntent { get; set; } = null!;

    public PaymentStatus Status { get; set; }

    [Required]
    [StringLength(100)]
    public required string EventType { get; set; } // payment.created, payment.captured, payment.failed, etc.

    [StringLength(4000)]
    public string? PayloadJson { get; set; }

    [StringLength(50)]
    public string? Source { get; set; } // API, Webhook, Admin, System

    [StringLength(50)]
    public string? IpAddress { get; set; }
}
