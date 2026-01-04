using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Orders.Application.Interfaces.Services;

namespace Orders.Infrastructure.Services;

public class WompiService : IWompiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;
    private readonly string _publicKey;
    private readonly string _privateKey;

    public WompiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _baseUrl = Environment.GetEnvironmentVariable("WOMPI_BASE_URL") ?? "https://production.wompi.co/v1";
        _publicKey = Environment.GetEnvironmentVariable("WOMPI_PUBLIC_KEY") ?? "";
        _privateKey = Environment.GetEnvironmentVariable("WOMPI_PRIVATE_KEY") ?? "";
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_publicKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<CreateTransactionResult> CreateTransactionAsync(CreateTransactionCommand command)
    {
        try
        {
            var requestBody = new
            {
                amount_in_cents = (int)(command.Amount * 100),
                currency = command.Currency,
                customer_email = "customer@example.com",
                payment_method = new
                {
                    type = command.PaymentMethod,
                    installments = 1
                },
                reference = command.Reference,
                redirect_url = Environment.GetEnvironmentVariable("WOMPI_REDIRECT_URL") ?? "https://your-app.com/payment/result"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/transactions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al crear transacción en Wompi: {responseContent}");
            }

            var wompiResponse = JsonSerializer.Deserialize<WompiTransactionResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (wompiResponse?.Data == null)
            {
                throw new Exception("Respuesta inválida de Wompi");
            }

            return new CreateTransactionResult(
                TransactionId: wompiResponse.Data.Id,
                Status: wompiResponse.Data.Status,
                RedirectUrl: wompiResponse.Data.RedirectUrl ?? ""
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al procesar transacción con Wompi: {ex.Message}", ex);
        }
    }

    private class WompiTransactionResponse
    {
        public WompiTransactionData? Data { get; set; }
    }

    private class WompiTransactionData
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public string? RedirectUrl { get; set; }
    }
}
