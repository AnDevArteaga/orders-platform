namespace Orders.Api.DTOs;

public record CreatePaymentRequest(decimal Amount, string Currency, string PaymentMethod);
public record CreatePaymentResponse(string TransactionId, string Status, string RedirectUrl);

