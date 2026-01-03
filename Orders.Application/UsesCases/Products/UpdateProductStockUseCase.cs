using Orders.Application.Interfaces.Repositories;
using Orders.Domain.Exceptions;

namespace Orders.Application.UsesCases.Products
{
    public class UpdateProductStockUseCase(IProductRepository productRepository)
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async Task ExecuteAsync(Guid productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId) ?? throw new DomainException("Product not found");
            product.IncreaseStock(quantity);
            await _productRepository.UpdateAsync(product);
            

        }
    }
}
