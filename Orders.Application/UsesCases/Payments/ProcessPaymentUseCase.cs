using Orders.Application.Interfaces.Repositories;
using Orders.Application.Interfaces.Services;
using Orders.Domain.Entities;

namespace Orders.Application.UsesCases.Payments;

public class ProcessPaymentUseCase
{
    private readonly IWompiService _wompiService;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;

    public ProcessPaymentUseCase(
        IWompiService wompiService,
        IPaymentTransactionRepository paymentTransactionRepository)
    {
        _wompiService = wompiService;
        _paymentTransactionRepository = paymentTransactionRepository;
    }

    public async Task<CreateTransactionResult> ExecuteAsync(
        decimal amount,
        string currency,
        string paymentMethod)
    {
        var reference = Guid.NewGuid().ToString();
        
        var command = new CreateTransactionCommand(
            Amount: amount,
            Currency: currency,
            PaymentMethod: paymentMethod,
            Reference: reference
        );

        var result = await _wompiService.CreateTransactionAsync(command);

        var transaction = new PaymentTransaction(
            transactionId: result.TransactionId,
            amount: amount,
            currency: currency,
            paymentMethod: paymentMethod
        );

        await _paymentTransactionRepository.AddAsync(transaction);

        return result;
    }
}

