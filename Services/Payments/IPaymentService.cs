using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Services.Payments;

public interface IPaymentService
{
    // Payment Intent
    Task<PaymentIntentResponseDto> CreateIntentAsync(CreatePaymentIntentDto dto, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> GetByIdAsync(int paymentIntentId, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default);

    // Payment Processing
    Task<PaymentIntentResponseDto> AuthorizeAsync(int paymentIntentId, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> CaptureAsync(int paymentIntentId, string? note = null, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> CancelAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> FailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default);

    // Refunds
    Task<RefundPaymentResponseDto> RefundAsync(int paymentIntentId, decimal? amount = null, string? reason = null, CancellationToken ct = default);

    // 3D Secure
    Task<PaymentIntentResponseDto> Confirm3DSecureAsync(int paymentIntentId, string authenticationResult, CancellationToken ct = default);

    // Webhook
    Task ProcessWebhookAsync(string provider, string payload, string? signature = null, CancellationToken ct = default);

    // Admin/Manual
    Task<PaymentIntentResponseDto> MarkReceivedAsync(int paymentIntentId, string? note = null, CancellationToken ct = default);

    // Simulation (for testing)
    Task<PaymentIntentResponseDto> SimulateSuccessAsync(int paymentIntentId, string? note = null, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> SimulateFailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default);

    // History
    Task<List<PaymentEventDto>> GetPaymentEventsAsync(int paymentIntentId, CancellationToken ct = default);
}
