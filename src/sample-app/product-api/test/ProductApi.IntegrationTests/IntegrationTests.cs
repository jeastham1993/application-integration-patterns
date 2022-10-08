using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace ProductApi.IntegrationTests;

internal class CreateProductResponse
{
    public string ProductId { get; set; }
}

public class IntegrationTests : IClassFixture<Setup>
{
    private readonly ITestOutputHelper output;
    private HttpClient _httpClient = new HttpClient();

    public IntegrationTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async void CreateAndRetrieve_ShouldReturnOk()
    {
        var createProductBody = new
        {
            Name = "Integration test product",
            Price = 10.00,
            Description = "This is a product created from an integration test"
        };

        var httpResponse = await this._httpClient.PostAsync(Setup.ApiUrl,
            new StringContent(JsonSerializer.Serialize(createProductBody), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response =
            JsonSerializer.Deserialize<CreateProductResponse>(await httpResponse.Content.ReadAsStringAsync());

        var getProductResponse = await this._httpClient.GetAsync($"{Setup.ApiUrl}{response.ProductId}");

        Assert.Equal(HttpStatusCode.OK, getProductResponse.StatusCode);
    }

    [Fact]
    public async void CreateUpdateDelete_ShouldReturnOk()
    {
        var createProductBody = new
        {
            Name = "Delete test product",
            Price = 10.00,
            Description = "This is a product created from an integration test"
        };

        var httpResponse = await this._httpClient.PostAsync(Setup.ApiUrl,
            new StringContent(JsonSerializer.Serialize(createProductBody), Encoding.UTF8, "application/json"));
        
        this.output.WriteLine($"CreateProductTrace: {httpResponse.Headers.GetValues("X_TRACE_ID").FirstOrDefault()}");
        
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response =
            JsonSerializer.Deserialize<CreateProductResponse>(await httpResponse.Content.ReadAsStringAsync());
        
        var updateProductBody = new
        {
            ProductId = response.ProductId,
            Name = "Delete test product",
            Price = 50.00,
            Description = "Updated product body"
        };

        var updateProductResponse = await this._httpClient.PutAsync($"{Setup.ApiUrl}{response.ProductId}",
            new StringContent(JsonSerializer.Serialize(updateProductBody), Encoding.UTF8, "application/json"));
        
        this.output.WriteLine($"UpdateProductTrace: {updateProductResponse.Headers.GetValues("X_TRACE_ID").FirstOrDefault()}");

        Assert.Equal(HttpStatusCode.OK, updateProductResponse.StatusCode);

        var deleteProductResponse = await this._httpClient.DeleteAsync($"{Setup.ApiUrl}{response.ProductId}");
        
        this.output.WriteLine($"DeletedProductTrace: {deleteProductResponse.Headers.GetValues("X_TRACE_ID").FirstOrDefault()}");

        Assert.Equal(HttpStatusCode.OK, deleteProductResponse.StatusCode);
    }
}