using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using SynchronousApi.Core.Models;

namespace SynchronousApi.Implementations;

public class DynamoDbProductRepository : IProductRepository
{
    private readonly AmazonDynamoDBClient _client;
    private static string PRODUCT_TABLE_NAME = Environment.GetEnvironmentVariable("PRODUCT_TABLE_NAME");

    public DynamoDbProductRepository(AmazonDynamoDBClient client)
    {
        _client = client;
    }
    
    public async Task Create(Product product)
    {
        var item = new Dictionary<string, AttributeValue>(3);
        item.Add("PK", new AttributeValue(product.ProductId));
        item.Add("Name", new AttributeValue(product.Name));
        item.Add("Description", new AttributeValue(product.Description));
        item.Add("Price", new AttributeValue()
        {
            N = product.Price.ToString(CultureInfo.InvariantCulture)
        });

        var pricingHistory = new Dictionary<string, AttributeValue>();

        foreach (var history in product.PricingHistory)
        {
            pricingHistory.Add(history.Date.ToString("O"), new AttributeValue()
            {
                N = history.Price.ToString(CultureInfo.InvariantCulture)
            });
        }
        
        item.Add("PricingHistory", new AttributeValue()
        {
            M = pricingHistory
        });
            
        await this._client.PutItemAsync(PRODUCT_TABLE_NAME, item);
    }

    public async Task<Product> Get(string productId)
    {
        var getItemResponse = await this._client.GetItemAsync(new GetItemRequest(PRODUCT_TABLE_NAME,
            new Dictionary<string, AttributeValue>(1)
            {
                {"PK", new AttributeValue(productId)}
            }));

        var pricingHistory = new Dictionary<DateTime, decimal>();

        foreach (var priceHistory in getItemResponse.Item["PricingHistory"].M)
        {
            pricingHistory.Add(DateTime.Parse(priceHistory.Key), decimal.Parse(priceHistory.Value.N));
        }

        return Product.Create(getItemResponse.Item["PK"].S,
            getItemResponse.Item["Name"].S,
            decimal.Parse(getItemResponse.Item["Price"].N),
            getItemResponse.Item["Description"].S,
            pricingHistory);
    }
}