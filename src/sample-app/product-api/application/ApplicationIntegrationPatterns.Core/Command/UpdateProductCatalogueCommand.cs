using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationIntegrationPatterns.Core.Command;

public record UpdateProductCatalogueCommand
{
    public ProductDTO Product { get; set; }

    internal bool ValidateProperties() => this.Product != null;
}

public class UpdateProductCatalogueCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ILoggingService _loggingService;
    private readonly IProductCatalogueService _productCatalogueService;

    public UpdateProductCatalogueCommandHandler(IProductRepository productRepository, ILoggingService loggingService,
        IProductCatalogueService productCatalogueService)
    {
        _productRepository = productRepository;
        _loggingService = loggingService;
        this._productCatalogueService = productCatalogueService;
    }

    public async Task Handle(UpdateProductCatalogueCommand command)
    {
        if (!isCommandValid(command))
        {
            this._loggingService.LogWarning("Invalid command, returning");
            return;
        }

        this._loggingService.LogInfo("Updating product catalogue from new Update Product command");

        var product = await this._productRepository.Get(command.Product.ProductId);

        if (product == null)
        {
            this._loggingService.LogWarning($"Product {command.Product.ProductId} not found");

            return;
        }

        using (var activity = Activity.Current?.Source.StartActivity("Updaing product catalogue"))
        {
            await this._productCatalogueService.UpdateProduct(product);
        }

        this._loggingService.LogInfo("Product catalogue updated");

        return;
    }

    private bool isCommandValid(UpdateProductCatalogueCommand command)
    {
        if (command == null)
        {
            return false;
        }

        return command.ValidateProperties();
    }
}