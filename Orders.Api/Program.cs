using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Orders.Api.Auth;
using Orders.Api.Errors;
using Orders.Api.Middlewares;
using Orders.Application.Abstractions;
using Orders.Application.Services;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Security;
using Orders.Infrastructure.Services;

if (File.Exists(".env"))
    Env.Load();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.SnakeCaseLower));
});

builder.Services.AddAuthentication("Bearer")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PlatformAuthHandler>("Bearer", null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Account", policy => policy.RequireAuthenticatedUser().RequireClaim("kind", "account"));
    options.AddPolicy("Integration", policy => policy.RequireAuthenticatedUser().RequireClaim("kind", "api_key"));
});

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Orders")));
builder.Services.AddScoped<IPlatformStore, EfPlatformStore>();
builder.Services.AddSingleton<ISecretProtector>(_ =>
    new AesSecretProtector(builder.Configuration["DataProtection:Key"]!));
builder.Services.AddHttpClient<IWebhookDispatcher, HttpWebhookDispatcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddHttpClient<IWompiClient, WompiClient>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IntegrationService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var permit = builder.Configuration.GetValue("RateLimit:PermitPerMinute", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("integration", httpContext =>
    {
        var key = httpContext.User.FindFirst("key_id")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Límite de consumo superado." }, token);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Orders Platform",
        Version = "v1",
        Description = "Pedidos y cobros para comercios. La tarjeta la escribe el comprador en Wompi. En sandbox no se llama a la pasarela."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT o API key",
        In = ParameterLocation.Header,
        Description = "Cuenta: el JWT del login. Integración: la API key, con el prefijo Bearer."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<RequestLogMiddleware>();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { ok = true }));
app.Run();

public partial class Program;
