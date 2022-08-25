using System.Threading.Tasks;
using SynchronousApi.Core.Command;
using SynchronousApi.Core.Models;
using SynchronousApi.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ProductApi.Test;

public class CommandTests
{
    private ILoggingService _mockLogger = new Mock<ILoggingService>().Object;

    [Fact]
    public async Task CreateProduct_WithValidCommand_ShouldBeSuccessful()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Create(It.IsAny<Product>())).Verifiable();

        var handler = new CreateProductCommandHandler(mockRepo.Object, _mockLogger);

        var createdProduct = await handler.Handle(new CreateProductCommand()
        {
            Name = "Test product",
            Price = 10,
            Description = "Look, the test product description"
        });

        createdProduct.Name.Should().Be("Test product");
        createdProduct.PricingHistory.Count.Should().Be(1);
        createdProduct.Description.Should().Be("Look, the test product description");
        mockRepo.Verify(p => p.Create(It.IsAny<Product>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateProduct_WithInvalidName_ShouldNotCreate()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Create(It.IsAny<Product>())).Verifiable();

        var handler = new CreateProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new CreateProductCommand()
        {
            Name = null,
            Price = 10
        });
        
        mockRepo.Verify(p => p.Create(It.IsAny<Product>()), Times.Never);
    }
    
    [Fact]
    public async Task CreateProduct_WithInvalidPrice_ShouldNotCreate()
    {
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Create(It.IsAny<Product>())).Verifiable();

        var handler = new CreateProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new CreateProductCommand()
        {
            Name = "Test product",
            Price = 0
        });
        
        mockRepo.Verify(p => p.Create(It.IsAny<Product>()), Times.Never);
    }
}