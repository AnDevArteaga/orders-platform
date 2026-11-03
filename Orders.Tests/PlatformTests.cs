using System.Net;
using Xunit;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Security;
using Orders.Application.Wompi;
using Orders.Domain.Entities;

namespace Orders.Tests;

public class PlatformTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PlatformTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public void Un_pedido_sin_pagar_no_se_puede_despachar()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), null, [("Audifonos", 1, 120_000m)]);
        var error = Assert.Throws<Domain.Exceptions.ConflictException>(order.Fulfill);
        Assert.Contains("pagado", error.Message);
    }

    [Fact]
    public void La_firma_del_webhook_cubre_el_cuerpo_y_el_tiempo()
    {
        var header = WebhookSigner.Header("secreto", 1_700_000_000, """{"ok":true}""");
        Assert.StartsWith("t=1700000000,v1=", header);
        Assert.Equal(
            WebhookSigner.Sign("secreto", 1_700_000_000, """{"ok":true}"""),
            header.Split("v1=")[1]);
    }

    [Fact]
    public async Task Sandbox_aprueba_el_pago_y_registra_el_evento()
    {
        var key = await SandboxKeyAsync();
        await PutWebhookAsync(key);

        var orderId = await CreateOrderAsync(key);
        var paid = await SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/payments", key, new { sandbox_result = "approved" });
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        var payment = await paid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", payment.GetProperty("status").GetString());
        Assert.Contains("/sandbox/checkout/", payment.GetProperty("checkout_url").GetString());

        var order = await GetJsonAsync($"/v1/orders/{orderId}", key);
        Assert.Equal("paid", order.GetProperty("status").GetString());
        Assert.Equal(120_000, order.GetProperty("total").GetDecimal());

        var events = await GetJsonAsync("/v1/events", key);
        Assert.Equal(1, events.GetProperty("count").GetInt32());
        Assert.Equal("order.paid", events.GetProperty("data")[0].GetProperty("type").GetString());
        Assert.False(events.GetProperty("data")[0].GetProperty("delivered").GetBoolean());
        Assert.Equal(3, events.GetProperty("data")[0].GetProperty("attempts").GetInt32());
    }

    [Fact]
    public async Task La_llave_de_produccion_no_cobra_hasta_activar_wompi()
    {
        var account = await LoginAsync();
        var sandbox = await IssueKeyAsync(account, "sandbox");
        var live = await IssueKeyAsync(account, "production");
        var orderId = await CreateOrderAsync(sandbox);

        var blocked = await SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/payments", live, new { });
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        var saved = await SendAsync(HttpMethod.Put, "/account/integrations/wompi", account, new
        {
            public_key = "pub_test",
            private_key = "prv_test",
            events_secret = "events_test_secret",
            target = "sandbox"
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var view = await GetJsonAsync("/account/integrations/wompi", account);
        Assert.True(view.GetProperty("configured").GetBoolean());
        Assert.False(view.GetProperty("activated").GetBoolean());
        Assert.False(view.TryGetProperty("public_key", out _));

        var activated = await SendAsync(HttpMethod.Post, "/account/integrations/wompi/activate", account, new { });
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);

        var started = await SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/payments", live, new { });
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var payment = await started.Content.ReadFromJsonAsync<JsonElement>();
        var linkId = payment.GetProperty("checkout_url").GetString()!.Split('/').Last();

        var wrong = await WompiEventAsync(linkId, 120_000_01, "APPROVED", "events_test_secret");
        Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
        var failed = await GetJsonAsync($"/v1/orders/{orderId}", live);
        Assert.Equal("payment_failed", failed.GetProperty("status").GetString());

        var again = await SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/payments", live, new { });
        var second = await again.Content.ReadFromJsonAsync<JsonElement>();
        var secondLink = second.GetProperty("checkout_url").GetString()!.Split('/').Last();
        var right = await WompiEventAsync(secondLink, 12_000_000, "APPROVED", "events_test_secret");
        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        var paid = await GetJsonAsync($"/v1/orders/{orderId}", live);
        Assert.Equal("paid", paid.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Una_firma_de_wompi_invalida_se_rechaza()
    {
        var account = await LoginAsync();
        var live = await IssueKeyAsync(account, "production");
        await SendAsync(HttpMethod.Put, "/account/integrations/wompi", account, new
        {
            public_key = "pub_test",
            private_key = "prv_test",
            events_secret = "events_test_secret",
            target = "sandbox"
        });
        await SendAsync(HttpMethod.Post, "/account/integrations/wompi/activate", account, new { });
        var orderId = await CreateOrderAsync(live);
        var started = await SendAsync(HttpMethod.Post, $"/v1/orders/{orderId}/payments", live, new { });
        var linkId = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("checkout_url").GetString()!.Split('/').Last();

        var response = await _client.PostAsJsonAsync("/webhooks/wompi", EventBody(linkId, 12_000_000, "APPROVED", "no-es-la-firma"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> SandboxKeyAsync()
    {
        var account = await LoginAsync();
        return await IssueKeyAsync(account, "sandbox");
    }

    private async Task<string> LoginAsync()
    {
        var email = $"tienda-{Guid.NewGuid():N}@example.com";
        var registered = await _client.PostAsJsonAsync("/account/register", new
        {
            name = "Tienda demo",
            email,
            password = "clave-demo-1"
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var login = await _client.PostAsJsonAsync("/account/login", new { email, password = "clave-demo-1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    private async Task<string> IssueKeyAsync(string accountToken, string environment)
    {
        var response = await SendAsync(HttpMethod.Post, "/account/api-keys", accountToken, new
        {
            name = environment,
            environment
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        Assert.StartsWith(environment == "sandbox" ? "sk_test_" : "sk_live_", token);

        var listed = await GetJsonAsync("/account/api-keys", accountToken);
        var serialized = listed.ToString();
        Assert.DoesNotContain(token, serialized);
        return token;
    }

    private async Task<Guid> CreateOrderAsync(string apiKey)
    {
        var customer = await SendAsync(HttpMethod.Post, "/v1/customers", apiKey, new
        {
            name = "Ana",
            email = $"ana-{Guid.NewGuid():N}@correo.com"
        });
        Assert.Equal(HttpStatusCode.Created, customer.StatusCode);
        var customerId = (await customer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var order = await SendAsync(HttpMethod.Post, "/v1/orders", apiKey, new
        {
            customer_id = customerId,
            reference = $"pedido-{Guid.NewGuid():N}",
            lines = new[] { new { name = "Audifonos", quantity = 1, unit_amount = 120_000 } }
        });
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);
        return (await order.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task PutWebhookAsync(string apiKey)
    {
        var response = await SendAsync(HttpMethod.Put, "/v1/webhook", apiKey, new { url = "http://127.0.0.1:9/hook" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("secret").GetString()));
    }

    private async Task<HttpResponseMessage> WompiEventAsync(string linkId, long cents, string status, string secret)
    {
        var body = EventBody(linkId, cents, status, "pending");
        var checksum = WompiSignature.Compute(body, secret);
        body = EventBody(linkId, cents, status, checksum);
        return await _client.PostAsJsonAsync("/webhooks/wompi", body);
    }

    private static JsonElement EventBody(string linkId, long cents, string status, string checksum)
    {
        var json = $$"""
        {
          "event": "transaction.updated",
          "data": {
            "transaction": {
              "id": "txn_1",
              "status": "{{status}}",
              "amount_in_cents": {{cents}},
              "payment_link_id": "{{linkId}}"
            }
          },
          "signature": {
            "properties": ["transaction.id", "transaction.status", "transaction.amount_in_cents"],
            "checksum": "{{checksum}}"
          },
          "timestamp": 1700000000
        }
        """;
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private async Task<JsonElement> GetJsonAsync(string path, string token)
    {
        var response = await SendAsync(HttpMethod.Get, path, token, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }
}
