namespace SynchronousApi.Core.Models;

public record PricingHistory(DateTime Date, decimal Price)
{
    public DateTime Date { get; private set; } = Date;

    public decimal Price { get; private set; } = Price;
}