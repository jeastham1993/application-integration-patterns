using System.Runtime.CompilerServices;

namespace SynchronousApi.Core.Models;

public class Product
{
    private List<PricingHistory> _pricingHistory;
    public static Product Create(string name, decimal price)
    {
        var product = new Product()
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = name,
            Price = price,
        };
        
        product._pricingHistory.Add(new PricingHistory(DateTime.Now, price));

        return product;
    }
    
    public static Product Create(string id, string name, decimal price, string description, Dictionary<DateTime, decimal> pricingHistory)
    {
        var product = new Product()
        {
            ProductId = id,
            Name = name,
            Price = price,
            Description = description
        };

        foreach (var history in pricingHistory)
        {
            product._pricingHistory.Add(new PricingHistory(history.Key, history.Value));
        }

        return product;
    }

    private Product()
    {
        this._pricingHistory = new List<PricingHistory>();
    }
    
    public string ProductId { get; private set; }
    
    public string Name { get; private set; }
    
    public decimal Price { get; private set; }
    
    public string Description { get; private set; }

    public IReadOnlyCollection<PricingHistory> PricingHistory => this._pricingHistory;
    
    public void UpdatePricing(decimal newPrice)
    {
        if (this.Price == newPrice)
        {
            return;
        }
        
        this._pricingHistory.Add(new PricingHistory(DateTime.Now, newPrice));

        this.Price = newPrice;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        this.Description = description;
    }
}