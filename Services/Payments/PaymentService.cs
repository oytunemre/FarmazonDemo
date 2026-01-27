using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FarmazonDemo.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public PaymentService(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<PaymentIntentResponseDto> CreateIntentAsync(CreatePaymentIntentDto dto, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);
        if (order is null)
            throw new NotFoundException($"Order not found. OrderId={dto.OrderId}");

        var existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId, ct);
        if (existing is not null)
            return Map(existing);

        var intent = new PaymentIntent
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = dto.Currency.ToUpperInvariant(),
            Method = dto.Method,
            Status = PaymentStatus.Created,
            Provider = GetProviderForMethod(dto.Method),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            InstallmentCount = dto.InstallmentCount,
            Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
        };

        // Handle card payments
        if (dto.CardDetails != null && (dto.Method == PaymentMethod.CreditCard || dto.Method == PaymentMethod.DebitCard))
        {
            intent.CardLast4 = dto.CardDetails.CardNumber.Length >= 4
                ? dto.CardDetails.CardNumber[^4..]
                : dto.CardDetails.CardNumber;
            intent.CardBrand = DetectCardBrand(dto.CardDetails.CardNumber);

            // Simulate 3D Secure requirement for amounts > 500
            if (order.TotalAmount > 500)
            {
                intent.Requires3DSecure = true;
                intent.ThreeDSecureUrl = $"/api/payments/{intent.Id}/3ds?returnUrl={dto.ReturnUrl}";
                intent.Status = PaymentStatus.Pending;
            }
        }

        // Calculate installment amount
        if (dto.InstallmentCount.HasValue && dto.InstallmentCount > 1)
        {
            intent.InstallmentAmount = Math.Round(order.TotalAmount / dto.InstallmentCount.Value, 2);
        }

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync(ct);

        await AddEventAsync(intent.Id, PaymentStatus.Created, "payment.created",
            JsonSerializer.Serialize(new { method = intent.Method.ToString(), amount = intent.Amount }), "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByIdAsync(int paymentIntentId, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found. Id={paymentIntentId}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found for OrderId={orderId}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.ExternalReference == externalReference, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found for ExternalReference={externalReference}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> AuthorizeAsync(int paymentIntentId, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Authorized);

        intent.Status = PaymentStatus.Authorized;
        intent.AuthorizedAt = DateTime.UtcNow;
        intent.ExternalReference = GenerateExternalReference();

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.authorized", null, "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> CaptureAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Captured);

        intent.Status = PaymentStatus.Captured;
        intent.CapturedAt = DateTime.UtcNow;
        intent.FailureReason = null;

        if (string.IsNullOrEmpty(intent.ExternalReference))
        {
            intent.ExternalReference = GenerateExternalReference();
        }

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.captured",
            note != null ? JsonSerializer.Serialize(new { note }) : null, "API", ct);

        // Update order status
        await UpdateOrderPaymentStatusAsync(intent.OrderId, true, ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Payment Successful",
                $"Your payment of {intent.Amount:N2} {intent.Currency} has been processed successfully."
            );
        }

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> CancelAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Cancelled);

        intent.Status = PaymentStatus.Cancelled;
        intent.FailureReason = reason;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.cancelled",
            reason != null ? JsonSerializer.Serialize(new { reason }) : null, "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> FailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Failed);

        intent.Status = PaymentStatus.Failed;
        intent.FailedAt = DateTime.UtcNow;
        intent.FailureReason = reason;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.failed",
            reason != null ? JsonSerializer.Serialize(new { reason }) : null, "API", ct);

        // Update order status
        await UpdateOrderPaymentStatusAsync(intent.OrderId, false, ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Payment Failed",
                $"Your payment could not be processed. Reason: {reason ?? "Unknown error"}"
            );
        }

        return Map(intent);
    }

    public async Task<RefundPaymentResponseDto> RefundAsync(int paymentIntentId, decimal? amount = null, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);

        if (intent.Status != PaymentStatus.Captured && intent.Status != PaymentStatus.PartiallyRefunded)
            throw new ValidationException("Only captured payments can be refunded");

        var refundAmount = amount ?? (intent.Amount - intent.RefundedAmount);
        var maxRefundable = intent.Amount - intent.RefundedAmount;

        if (refundAmount <= 0 || refundAmount > maxRefundable)
            throw new ValidationException($"Invalid refund amount. Maximum refundable: {maxRefundable:N2}");

        intent.RefundedAmount += refundAmount;
        intent.RefundedAt = DateTime.UtcNow;

        // Determine new status
        if (intent.RefundedAmount >= intent.Amount)
        {
            intent.Status = PaymentStatus.Refunded;
        }
        else
        {
            intent.Status = PaymentStatus.PartiallyRefunded;
        }

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.refunded",
            JsonSerializer.Serialize(new { amount = refundAmount, reason }), "API", ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Refund Processed",
                $"A refund of {refundAmount:N2} {intent.Currency} has been processed."
            );
        }

        return new RefundPaymentResponseDto
        {
            PaymentIntentId = intent.Id,
            RefundedAmount = refundAmount,
            TotalRefundedAmount = intent.RefundedAmount,
            RemainingAmount = intent.Amount - intent.RefundedAmount,
            Status = intent.Status,
            ExternalReference = intent.ExternalReference,
            RefundedAt = intent.RefundedAt ?? DateTime.UtcNow
        };
    }

    public async Task<PaymentIntentResponseDto> Confirm3DSecureAsync(int paymentIntentId, string authenticationResult, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);

        if (!intent.Requires3DSecure)
            throw new ValidationException("This payment does not require 3D Secure");

        if (intent.Status != PaymentStatus.Pending)
            throw new ValidationException("Payment is not awaiting 3D Secure confirmation");

        // Simulate 3D Secure result
        if (authenticationResult.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            intent.Status = PaymentStatus.Authorized;
            intent.AuthorizedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, intent.Status, "3ds.authenticated", null, "3DSecure", ct);

            // Auto-capture after successful 3DS
            return await CaptureAsync(paymentIntentId, "Auto-captured after 3DS", ct);
        }
        else
        {
            intent.Status = PaymentStatus.Failed;
            intent.FailedAt = DateTime.UtcNow;
            intent.FailureReason = "3D Secure authentication failed";
            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, intent.Status, "3ds.failed",
                JsonSerializer.Serialize(new { result = authenticationResult }), "3DSecure", ct);
        }

        return Map(intent);
    }

    public async Task ProcessWebhookAsync(string provider, string payload, string? signature = null, CancellationToken ct = default)
    {
        // In a real implementation, verify the signature
        // For now, just parse and process the webhook

        var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (webhookEvent == null || string.IsNullOrEmpty(webhookEvent.ExternalReference))
            throw new ValidationException("Invalid webhook payload");

        var intent = await _db.PaymentIntents
            .FirstOrDefaultAsync(x => x.ExternalReference == webhookEvent.ExternalReference, ct);

        if (intent == null)
        {
            // Log unknown webhook - payment intent not found
            return;
        }

        // Process based on event type
        switch (webhookEvent.EventType?.ToLower())
        {
            case "payment.captured":
                await CaptureAsync(intent.Id, "Captured via webhook", ct);
                break;
            case "payment.failed":
                await FailAsync(intent.Id, webhookEvent.Message, ct);
                break;
            case "payment.refunded":
                await RefundAsync(intent.Id, webhookEvent.Amount, webhookEvent.Message, ct);
                break;
        }

        await AddEventAsync(intent.Id, intent.Status, $"webhook.{webhookEvent.EventType}",
            payload, "Webhook", ct);
    }

    public async Task<PaymentIntentResponseDto> MarkReceivedAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
    {
        return await CaptureAsync(paymentIntentId, note ?? "Manually marked as received", ct);
    }

    public Task<PaymentIntentResponseDto> SimulateSuccessAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
        => CaptureAsync(paymentIntentId, note ?? "Simulated success", ct);

    public async Task<PaymentIntentResponseDto> SimulateFailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
        => await FailAsync(paymentIntentId, reason ?? "Simulated failure", ct);

    public async Task<List<PaymentEventDto>> GetPaymentEventsAsync(int paymentIntentId, CancellationToken ct = default)
    {
        return await _db.PaymentEvents
            .Where(e => e.PaymentIntentId == paymentIntentId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new PaymentEventDto
            {
                Id = e.Id,
                PaymentIntentId = e.PaymentIntentId,
                Status = e.Status,
                EventType = e.EventType,
                PayloadJson = e.PayloadJson,
                Source = e.Source,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);
    }

    // Private helpers

    private async Task<PaymentIntent> GetIntentAsync(int paymentIntentId, CancellationToken ct)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found. Id={paymentIntentId}");
        return intent;
    }

    private static void EnsureCanTransitionTo(PaymentIntent intent, PaymentStatus targetStatus)
    {
        var validTransitions = new Dictionary<PaymentStatus, PaymentStatus[]>
        {
            { PaymentStatus.Created, new[] { PaymentStatus.Pending, PaymentStatus.Authorized, PaymentStatus.Captured, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Pending, new[] { PaymentStatus.Authorized, PaymentStatus.Captured, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Authorized, new[] { PaymentStatus.Captured, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Captured, new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded } },
            { PaymentStatus.PartiallyRefunded, new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded } }
        };

        if (!validTransitions.TryGetValue(intent.Status, out var allowed) || !allowed.Contains(targetStatus))
        {
            throw new ValidationException($"Cannot transition from {intent.Status} to {targetStatus}");
        }
    }

    private async Task AddEventAsync(int paymentIntentId, PaymentStatus status, string eventType, string? payloadJson, string source, CancellationToken ct)
    {
        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentIntentId = paymentIntentId,
            Status = status,
            EventType = eventType,
            PayloadJson = payloadJson,
            Source = source
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpdateOrderPaymentStatusAsync(int orderId, bool success, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync(new object[] { orderId }, ct);
        if (order == null) return;

        if (success)
        {
            order.Status = OrderStatus.Processing;
            order.PaidAt = DateTime.UtcNow;
        }
        else
        {
            order.Status = OrderStatus.Failed;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static PaymentIntentResponseDto Map(PaymentIntent x) => new()
    {
        Id = x.Id,
        OrderId = x.OrderId,
        Amount = x.Amount,
        RefundedAmount = x.RefundedAmount,
        Currency = x.Currency,
        Method = x.Method,
        Status = x.Status,
        Provider = x.Provider,
        ExternalReference = x.ExternalReference,
        FailureReason = x.FailureReason,
        CardLast4 = x.CardLast4,
        CardBrand = x.CardBrand,
        Requires3DSecure = x.Requires3DSecure,
        ThreeDSecureUrl = x.ThreeDSecureUrl,
        InstallmentCount = x.InstallmentCount,
        InstallmentAmount = x.InstallmentAmount,
        AuthorizedAt = x.AuthorizedAt,
        CapturedAt = x.CapturedAt,
        FailedAt = x.FailedAt,
        RefundedAt = x.RefundedAt,
        ExpiresAt = x.ExpiresAt,
        CreatedAt = x.CreatedAt
    };

    private static string GetProviderForMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard or PaymentMethod.DebitCard or PaymentMethod.Installment => "STRIPE",
            PaymentMethod.Wallet => "PAYPAL",
            PaymentMethod.BankTransfer => "BANK",
            PaymentMethod.BuyNowPayLater => "KLARNA",
            _ => "MANUAL"
        };
    }

    private static string DetectCardBrand(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) return "Unknown";

        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

        if (cleanNumber.StartsWith('4')) return "Visa";
        if (cleanNumber.StartsWith('5')) return "Mastercard";
        if (cleanNumber.StartsWith("34") || cleanNumber.StartsWith("37")) return "Amex";
        if (cleanNumber.StartsWith("6011")) return "Discover";

        return "Unknown";
    }

    private static string GenerateExternalReference()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private class WebhookEvent
    {
        public string? EventType { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Message { get; set; }
    }
}
