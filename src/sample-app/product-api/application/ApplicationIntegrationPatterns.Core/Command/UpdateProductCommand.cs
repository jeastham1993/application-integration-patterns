using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;

namespace ApplicationIntegrationPatterns.Core.Command;

public record UpdateProductCommand
{
    public string ProductId { get; set; }
    
    public string Name { get; set; }
    
    public decimal Price { get; set; }
    
    public string Description { get; set; }

    internal bool ValidateProperties()
    {
        return !string.IsNullOrEmpty(ProductId);
    }
}

public class UpdateProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ILoggingService _loggingService;

    public UpdateProductCommandHandler(IProductRepository productRepository, ILoggingService loggingService)
    {
        _productRepository = productRepository;
        _loggingService = loggingService;
    }

    public async Task<ProductDTO> Handle(UpdateProductCommand command)
    {
        if (!isCommandValid(command))
        {
            return null;
        }

        this._loggingService.LogInfo("Retrieving product from database");

        var product = await this._productRepository.Get(command.ProductId);
        
        this._loggingService.LogInfo("Updating data fields");
        
        product.UpdateDescription(command.Description);
        product.UpdatePricing(command.Price);

        if (!product.HasChanged)
        {
            this._loggingService.LogInfo("No changes required");
            
            return new ProductDTO(product);
        }
            
        await this._productRepository.Update(product);

        this._loggingService.LogInfo("Product updated");

        return new ProductDTO(product);
    }

    private bool isCommandValid(UpdateProductCommand command)
    {
        if (command == null)
        {
            return false;
        }

        return command.ValidateProperties();
    }
}