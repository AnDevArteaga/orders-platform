using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<WompiIntegration> WompiIntegrations => Set<WompiIntegration>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasIndex(merchant => merchant.Email).IsUnique();
            entity.Property(merchant => merchant.Name).HasMaxLength(120);
            entity.Property(merchant => merchant.Email).HasMaxLength(200);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(key => key.Hash).IsUnique();
            entity.Property(key => key.Environment).HasConversion<string>();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(customer => new { customer.MerchantId, customer.Email }).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(order => order.Status).HasConversion<string>();
            entity.Property(order => order.Total).HasPrecision(18, 2);
            entity.HasMany(order => order.Lines)
                .WithOne()
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(order => order.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(order => order.History)
                .WithOne()
                .HasForeignKey(change => change.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(order => order.History)
                .HasField("_history")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(line => line.UnitAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderStatusChange>(entity =>
        {
            entity.Property(change => change.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.Status).HasConversion<string>();
            entity.Property(payment => payment.Environment).HasConversion<string>();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.HasIndex(payment => payment.ProviderId);
        });

        modelBuilder.Entity<WebhookEndpoint>(entity =>
        {
            entity.HasKey(endpoint => endpoint.MerchantId);
        });

        modelBuilder.Entity<WompiIntegration>(entity =>
        {
            entity.HasKey(integration => integration.MerchantId);
            entity.Property(integration => integration.Target).HasConversion<string>();
            entity.Ignore(integration => integration.IsConfigured);
        });

        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(log => log.Id);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var id = entityType.FindProperty("Id");
            if (id?.ClrType == typeof(Guid))
                id.ValueGenerated = ValueGenerated.Never;
        }
    }
}
