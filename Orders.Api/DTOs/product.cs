namespace Orders.Api.DTOs;
public record CreateProductRequest(string Name, decimal Price, int Stock);
public record UpdateStockRequest(int Quantity);
