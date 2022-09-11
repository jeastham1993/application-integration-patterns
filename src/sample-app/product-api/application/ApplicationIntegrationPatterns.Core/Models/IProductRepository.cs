namespace ApplicationIntegrationPatterns.Core.Models;

public interface IProductRepository
{
    Task Create(Product product);

    Task<Product> Get(string productId);

    Task<Product> Update(Product product);

    Task Delete(string productId);
}