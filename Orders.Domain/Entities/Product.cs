
using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class Product : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    private Product() { }
    public Product(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        
            throw new DomainException("Product name cannot be empty.");
        
        if (price <= 0)
        
            throw new DomainException("Product price cannot be negative.");
        
        if (stock < 0)
        
            throw new DomainException("Product stock cannot be negative.");
        
        Name = name;
        Price = price;
        Stock = stock;

    }
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("Product price cannot be negative.");
        Price = newPrice;
    }
    public void DecreaseStock(int quantity)
    {
       if (quantity < 0) 
            throw new DomainException("Quantity to decrease cannot be negative.");
       if (quantity > Stock)
            throw new DomainException("Insufficient stock to decrease.");

        Stock -= quantity;
    }
    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity to increase cannot be negative.");
        Stock += quantity;
    }
}
