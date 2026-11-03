using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Abstractions;
using Orders.Domain.Exceptions;

namespace Orders.Infrastructure.Services;

public sealed class WompiClient(HttpClient http) : IWompiClient
{
    public async Task<string> CreatePaymentLinkAsync(WompiLinkRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{request.BaseUrl.TrimEnd('/')}/payment_links");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.PrivateKey);
        message.Content = JsonContent.Create(new
        {
            name = request.Name,
            description = "Pago del pedido",
            single_use = true,
            collect_shipping = false,
            currency = request.Currency,
            amount_in_cents = request.AmountInCents,
            redirect_url = request.RedirectUrl
        });

        var response = await http.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new DomainException("Wompi rechazó la creación del enlace de pago.");

        using var document = JsonDocument.Parse(body);
        var id = document.RootElement.GetProperty("data").GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Wompi no devolvió el identificador del enlace.");
        return id;
    }
}
