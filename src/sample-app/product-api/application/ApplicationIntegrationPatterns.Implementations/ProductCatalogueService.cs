using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationIntegrationPatterns.Implementations
{
    public class ProductCatalogueService : IProductCatalogueService
    {
        public async Task UpdateProduct(Product product)
        {
            // Implementation to be added, faking work
            await Task.Delay(100);

            return;
        }
    }
}
