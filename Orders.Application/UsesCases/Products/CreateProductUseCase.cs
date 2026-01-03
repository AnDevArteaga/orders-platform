using Orders.Application.Interfaces.Repositories;
using Orders.Domain.Entities;

namespace Orders.Application.UsesCases.Products
{
    public class CreateProductUseCase
    {
        private readonly IProductRepository _productRepository;
        public CreateProductUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<Guid> ExecuteAsync(string name, decimal price, int stock)
        {
            var product = new Product(name, price, stock);
            await _productRepository.AddAsync(product);
            return product.Id;
        } 
    }
}
