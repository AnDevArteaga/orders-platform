using Microsoft.AspNetCore.Mvc;
using Orders.Application.UsesCases.Payments;
using Orders.Api.DTOs;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController(ProcessPaymentUseCase processPayment) : ControllerBase
{
    private readonly ProcessPaymentUseCase _processPayment = processPayment;

    [HttpPost]
    public async Task<IActionResult> CreatePayment(CreatePaymentRequest request)
    {
        try
        {
            var result = await _processPayment.ExecuteAsync(
                request.Amount,
                request.Currency,
                request.PaymentMethod
            );

            var response = new CreatePaymentResponse(
                TransactionId: result.TransactionId,
                Status: result.Status,
                RedirectUrl: result.RedirectUrl
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

