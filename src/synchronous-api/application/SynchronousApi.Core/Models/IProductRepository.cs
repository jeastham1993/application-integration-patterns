namespace SynchronousApi.Core.Models;

public interface IProductRepository
{
    Task Create(Product product);

    Task<Product> Get(string productId);
}