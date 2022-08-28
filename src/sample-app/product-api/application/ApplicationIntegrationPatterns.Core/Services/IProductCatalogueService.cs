using ApplicationIntegrationPatterns.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationIntegrationPatterns.Core.Services
{
    public interface IProductCatalogueService
    {
        Task UpdateProduct(Product product);
    }
}
