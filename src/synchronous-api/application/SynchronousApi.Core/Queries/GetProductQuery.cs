using SynchronousApi.Core.DataTransfer;
using SynchronousApi.Core.Models;

namespace SynchronousApi.Core.Queries;

public record GetProductQuery(string ProductId) { }

public class GetProductQueryHandler
{
    private readonly IProductRepository _repo;

    public GetProductQueryHandler(IProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<ProductDTO?> Execute(GetProductQuery query)
    {
        var product = await this._repo.Get(query.ProductId);

        if (product == null)
        {
            return null;
        }

        return new ProductDTO(product);
    }
}