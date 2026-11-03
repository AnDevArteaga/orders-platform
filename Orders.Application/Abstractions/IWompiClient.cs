namespace Orders.Application.Abstractions;

public sealed record WompiLinkRequest(
    string PrivateKey,
    string BaseUrl,
    string Name,
    long AmountInCents,
    string Currency,
    string RedirectUrl);

public interface IWompiClient
{
    Task<string> CreatePaymentLinkAsync(WompiLinkRequest request, CancellationToken cancellationToken);
}
