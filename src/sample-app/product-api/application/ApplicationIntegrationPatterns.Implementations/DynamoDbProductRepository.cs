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

    public async Task<Product> Update(Product product)
    {
        if (string.IsNullOrEmpty(product.ProductId))
        {
            return product;
        }

        var item = DynamoDbProductAdapter.ProductToDynamoDbItemUpdate(product);

        await this._client.UpdateItemAsync(PRODUCT_TABLE_NAME, new Dictionary<string, AttributeValue>(1)
        {
            {"PK", new AttributeValue(product.ProductId)}
        }, item);

        return product;
    }

    public async Task Delete(string productId)
    {
        var deleteItemResponse = await this._client.DeleteItemAsync(new DeleteItemRequest(PRODUCT_TABLE_NAME,
            new Dictionary<string, AttributeValue>(1)
            {
                {"PK", new AttributeValue(productId)}
            }));
    }
}