using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ApplicationIntegrationPatterns.Core.Models;

namespace ApplicationIntegrationPatterns.Implementations;

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
        var item = DynamoDbProductAdapter.ProductToDynamoDbItem(product);
                
        await this._client.PutItemAsync(PRODUCT_TABLE_NAME, item);
    }

    public async Task<Product> Get(string productId)
    {
        var getItemResponse = await this._client.GetItemAsync(new GetItemRequest(PRODUCT_TABLE_NAME,
            new Dictionary<string, AttributeValue>(1)
            {
                {"PK", new AttributeValue(productId)}
            }));

        return DynamoDbProductAdapter.DynamoDbItemToProduct(getItemResponse.Item);
    }
}