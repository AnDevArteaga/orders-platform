using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using Orders.Domain.Entities;

namespace Orders.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("sandbox/checkout")]
public class SandboxCheckoutController(PaymentService payments) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Page(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.ResolveSandboxAsync(id, "pending", null, cancellationToken);
        return Content(PageHtml(payment.Id, payment.Amount, payment.Status), "text/html");
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Submit(Guid id, [FromForm] string result, CancellationToken cancellationToken)
    {
        var payment = await payments.ResolveSandboxAsync(id, result, null, cancellationToken);
        return Content(PageHtml(payment.Id, payment.Amount, payment.Status), "text/html");
    }

    private static string PageHtml(Guid id, decimal amount, PaymentStatus status) =>
        $$"""
        <!doctype html>
        <meta charset="utf-8">
        <title>Pago de prueba</title>
        <style>
          body { background: #fff; color: #111; font: 16px/1.5 sans-serif; margin: 2rem; }
          button { font: inherit; margin-right: 0.5rem; }
        </style>
        <h1>Pago de prueba</h1>
        <p>Este cobro es de sandbox. No pasa por un banco ni por Wompi.</p>
        <p>Monto: {{amount.ToString("0", CultureInfo.InvariantCulture)}} COP. Estado: {{status.ToString().ToLowerInvariant()}}.</p>
        <form method="post" action="/sandbox/checkout/{{id}}">
          <button name="result" value="approved" type="submit">Aprobar</button>
          <button name="result" value="declined" type="submit">Rechazar</button>
        </form>
        """;
}
