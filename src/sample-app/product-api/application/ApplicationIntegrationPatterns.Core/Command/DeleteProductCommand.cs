using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;

namespace ApplicationIntegrationPatterns.Core.Command;

public record DeleteProductCommand
{
    public string ProductId { get; set; }

    internal bool ValidateProperties()
    {
        return !string.IsNullOrEmpty(ProductId);
    }
}

public class DeleteProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ILoggingService _loggingService;

    public DeleteProductCommandHandler(IProductRepository productRepository, ILoggingService loggingService)
    {
        _productRepository = productRepository;
        _loggingService = loggingService;
    }

    public async Task Handle(DeleteProductCommand command)
    {
        if (!isCommandValid(command))
        {
            return;
        }

        this._loggingService.LogInfo("Deleting product");
            
        await this._productRepository.Delete(command.ProductId);

        this._loggingService.LogInfo("Product deleted");
    }

    private bool isCommandValid(DeleteProductCommand command)
    {
        if (command == null)
        {
            return false;
        }

        return command.ValidateProperties();
    }
}