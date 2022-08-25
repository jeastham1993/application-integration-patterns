using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;

namespace ApplicationIntegrationPatterns.Core.Command;

public record CreateProductCommand
{
    public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public string Description { get; set; }

    internal bool ValidateProperties()
    {
        return !string.IsNullOrEmpty(Name) && Price > 0;
    }
}

public class CreateProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ILoggingService _loggingService;

    public CreateProductCommandHandler(IProductRepository productRepository, ILoggingService loggingService)
    {
        _productRepository = productRepository;
        _loggingService = loggingService;
    }

    public async Task<ProductDTO> Handle(CreateProductCommand command)
    {
        if (!isCommandValid(command))
        {
            return null;
        }

        this._loggingService.LogInfo("Creating product from new command");

        var product = Product.Create(command.Name, command.Price);
        
        product.UpdateDescription(command.Description);

        await this._productRepository.Create(product);

        this._loggingService.LogInfo("Product created");

        return new ProductDTO(product);
    }

    private bool isCommandValid(CreateProductCommand command)
    {
        if (command == null)
        {
            return false;
        }

        return command.ValidateProperties();
    }
}