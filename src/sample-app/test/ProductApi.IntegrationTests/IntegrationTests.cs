using System.Net;
using System.Text;
using System.Text.Json;

namespace ProductApi.IntegrationTests;

internal class CreateProductResponse
{
    public string ProductId { get; set; }
}

public class IntegrationTests : IClassFixture<Setup>
{
    private HttpClient _httpClient = new HttpClient();

    [Fact]
    public async void GetCallingIp_ShouldReturnOk()
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
}