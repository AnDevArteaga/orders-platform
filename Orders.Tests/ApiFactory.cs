using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application.Abstractions;

namespace Orders.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"orders-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", $"Data Source={_database}");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWompiClient>();
            services.AddSingleton<IWompiClient, FakeWompi>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDelete(_database);
        TryDelete(_database + "-wal");
        TryDelete(_database + "-shm");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}

public sealed class FakeWompi : IWompiClient
{
    public Task<string> CreatePaymentLinkAsync(WompiLinkRequest request, CancellationToken cancellationToken) =>
        Task.FromResult($"plink_{Guid.NewGuid():N}");
}
