using Microsoft.AspNetCore.Mvc;
using Orders.Application.UsesCases.Products;
using Orders.Api.DTOs;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController(CreateProductUseCase createProduct, UpdateProductStockUseCase updateStock) : ControllerBase
{
    private readonly CreateProductUseCase _createProduct = createProduct;
    private readonly UpdateProductStockUseCase _updateStock = updateStock;

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var productId = await _createProduct.ExecuteAsync(request.Name, request.Price, request.Stock);
        return CreatedAtAction(nameof(Create), new { id = productId });
    }
    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateStock(Guid id, UpdateStockRequest request)
    {
        await _updateStock.ExecuteAsync(id, request.Quantity);
        return NoContent();

    }
}
