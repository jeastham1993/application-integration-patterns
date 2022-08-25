using SynchronousApi.Core.Models;

namespace SynchronousApi.Core.DataTransfer;

public record ProductDTO
{
    public ProductDTO(Product product)
    {
        this.ProductId = product.ProductId;
        this.Name = product.Name;
        this.Description = product.Description;
        this.CurrentPrice = product.Price;
        this.PricingHistory = new Dictionary<DateTime, decimal>();
        
        foreach (var pricingHistory in product.PricingHistory)
        {
            this.PricingHistory.Add(pricingHistory.Date, pricingHistory.Price);
        }
    }
    
    public string ProductId { get; private set; }
    
    public string Name { get; private set; }
    
    public string Description { get; private set; }
    
    public decimal CurrentPrice { get; private set; }
    
    public Dictionary<DateTime, decimal> PricingHistory { get; private set; }
}