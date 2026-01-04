namespace Orders.Application.Interfaces.Services;

public record CreateTransactionCommand(decimal Amount, string Currency, string PaymentMethod, string Reference);
public record CreateTransactionResult(string TransactionId, string Status, string RedirectUrl);
public interface IWompiService
{
    Task<CreateTransactionResult> CreateTransactionAsync(CreateTransactionCommand command);
}
