using System.Threading.Tasks;
using SynchronousApi.Core.Models;
using SynchronousApi.Core.Queries;
using FluentAssertions;
using Moq;
using Xunit;

namespace ProductApi.Test;

public class QueryTests
{
    [Fact]
    public async Task RunProductQuery_ShouldReturnDTO()
    {
        var testProduct = Product.Create("Test product", 10);
        
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>())).ReturnsAsync(testProduct);

        var handler = new GetProductQueryHandler(mockRepo.Object);

        var queryResult = await handler.Execute(new GetProductQuery(testProduct.ProductId));

        queryResult.Name.Should().Be("Test product");
    }
    
    [Fact]
    public async Task CanCreateQuery_ShouldSetProperty()
    {
        var testProduct = Product.Create("Test product", 10);

        var query = new GetProductQuery(testProduct.ProductId);

        query.ProductId.Should().Be(testProduct.ProductId);
    }
    
    [Fact]
    public async Task RunProductQuery_RepoReturnsNull_ShouldReturnNull()
    {
        var testProduct = Product.Create("Test product", 10);
        
        var mockRepo = new Mock<IProductRepository>();

        var handler = new GetProductQueryHandler(mockRepo.Object);

        var queryResult = await handler.Execute(new GetProductQuery(testProduct.ProductId));

        queryResult.Should().BeNull();
    }
}