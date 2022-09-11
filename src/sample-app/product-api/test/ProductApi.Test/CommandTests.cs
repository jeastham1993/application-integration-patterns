using System.Threading.Tasks;
using ApplicationIntegrationPatterns.Core.Command;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;
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

    [Fact]
    public async Task UpdateProductWithNewDescription_ShouldUpdate()
    {
        var testProduct = Product.Create("Test product", 10);
        testProduct.UpdateDescription("Initial description");

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();
        mockRepo.Setup(p => p.Update(It.IsAny<Product>()))
            .Verifiable();

        var handler = new UpdateProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new UpdateProductCommand()
        {
            ProductId = testProduct.ProductId,
            Description = "My new description",
            Price = 10
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Once);
        mockRepo.Verify(p => p.Update(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductWithNewPrice_ShouldUpdate()
    {
        var testProduct = Product.Create("Test product", 10);
        testProduct.UpdateDescription("Initial description");

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();
        mockRepo.Setup(p => p.Update(It.IsAny<Product>()))
            .Verifiable();

        var handler = new UpdateProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new UpdateProductCommand()
        {
            ProductId = testProduct.ProductId,
            Description = "Initial description",
            Price = 50
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Once);
        mockRepo.Verify(p => p.Update(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductWithNoChanges_ShouldNotUpdate()
    {
        var testProduct = Product.Create("Test product", 10);

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();
        mockRepo.Setup(p => p.Update(It.IsAny<Product>()))
            .Verifiable();

        var handler = new UpdateProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new UpdateProductCommand()
        {
            ProductId = testProduct.ProductId,
            Description = null,
            Price = 10
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Once);
        mockRepo.Verify(p => p.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductCommand_ShouldDelete()
    {
        var testProduct = Product.Create("Test product", 10);

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();
        mockRepo.Setup(p => p.Delete(It.IsAny<string>()))
            .Verifiable();

        var handler = new DeleteProductCommandHandler(mockRepo.Object, _mockLogger);

        await handler.Handle(new DeleteProductCommand()
        {
            ProductId = testProduct.ProductId,
        });

        mockRepo.Verify(p => p.Delete(testProduct.ProductId), Times.Once);
    }

    [Fact]
    public async Task UpdateProductCatalog_ShouldUpdateCatalogue()
    {
        var testProduct = Product.Create("Test product", 10);

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();

        var mockCatalogueService = new Mock<IProductCatalogueService>();
        mockCatalogueService.Setup(p => p.UpdateProduct(It.IsAny<Product>())).Verifiable();

        var handler = new UpdateProductCatalogueCommandHandler(mockRepo.Object, _mockLogger, mockCatalogueService.Object);

        await handler.Handle(new UpdateProductCatalogueCommand()
        {
            Product = new ProductDTO(testProduct)
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Once);
        mockCatalogueService.Verify(p => p.UpdateProduct(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductCatalog_InvalidCommand_ShouldReturn()
    {
        var testProduct = Product.Create("Test product", 10);

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .ReturnsAsync(testProduct)
            .Verifiable();

        var mockCatalogueService = new Mock<IProductCatalogueService>();
        mockCatalogueService.Setup(p => p.UpdateProduct(It.IsAny<Product>())).Verifiable();

        var handler = new UpdateProductCatalogueCommandHandler(mockRepo.Object, _mockLogger, mockCatalogueService.Object);

        await handler.Handle(new UpdateProductCatalogueCommand()
        {
            Product = null
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Never);
        mockCatalogueService.Verify(p => p.UpdateProduct(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductCatalog_ProductNotFound_ShouldReturn()
    {
        var testProduct = Product.Create("Test product", 10);

        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(p => p.Get(It.IsAny<string>()))
            .Verifiable();

        var mockCatalogueService = new Mock<IProductCatalogueService>();
        mockCatalogueService.Setup(p => p.UpdateProduct(It.IsAny<Product>())).Verifiable();

        var handler = new UpdateProductCatalogueCommandHandler(mockRepo.Object, _mockLogger, mockCatalogueService.Object);

        await handler.Handle(new UpdateProductCatalogueCommand()
        {
            Product = null
        });

        mockRepo.Verify(p => p.Get(testProduct.ProductId), Times.Never);
        mockCatalogueService.Verify(p => p.UpdateProduct(It.IsAny<Product>()), Times.Never);
    }
}