using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using SynchronousApi.Core.Models;
using SynchronousApi.Core.Queries;
using SynchronousApi.Core.Services;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace ProductApi.Test
{
    public class FunctionTests
    {
        public FunctionTests()
        {
        }

        [Fact]
        public async Task GetProductHandler_ShouldReturnSuccess()
        {
            var testProduct = Product.Create("Test product", 10);
        
            var mockRepo = new Mock<IProductRepository>();
            mockRepo.Setup(p => p.Get(It.IsAny<string>())).ReturnsAsync(testProduct);

            var mockLogger = new Mock<ILoggingService>();

            var handler = new GetProductQueryHandler(mockRepo.Object);

            var getProductFunction = new GetProduct.Function(handler, mockLogger.Object);

            var queryResult = await getProductFunction.FunctionHandler(
                JsonConvert.DeserializeObject<APIGatewayProxyRequest>(EventHelper.ValidGetProductRequest),
                new TestLambdaContext());

            queryResult.StatusCode.Should().Be(200);
            queryResult.Body.Should().NotBeNull();
        }
    }
}