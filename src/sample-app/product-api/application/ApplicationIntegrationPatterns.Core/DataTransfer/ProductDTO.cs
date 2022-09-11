using ApplicationIntegrationPatterns.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApplicationIntegrationPatterns.Core.DataTransfer;

public record ProductDTO
{
    [JsonConstructor]
    private ProductDTO()
    {
    }
    
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
    
    public string ProductId { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public decimal CurrentPrice { get; set; }
    
    public Dictionary<DateTime, decimal> PricingHistory { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this);
}