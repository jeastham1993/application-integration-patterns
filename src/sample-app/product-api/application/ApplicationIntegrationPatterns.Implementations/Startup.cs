using Amazon.DynamoDBv2;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using ApplicationIntegrationPatterns.Core.Command;
using ApplicationIntegrationPatterns.Core.Models;
using ApplicationIntegrationPatterns.Core.Queries;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationIntegrationPatterns.Implementations;

public static class Startup
{
    public static ServiceProvider Services { get; private set; }

    public static void ConfigureServices()
    {
        var serviceCollection = new ServiceCollection();

        AWSSDKHandler.RegisterXRayForAllServices();

        serviceCollection.AddSingleton<ILoggingService, LoggingService>();
        serviceCollection.AddSingleton(new AmazonDynamoDBClient(new AmazonDynamoDBConfig()));
        serviceCollection.AddTransient<IProductRepository, DynamoDbProductRepository>();
        serviceCollection.AddSingleton<GetProductQueryHandler>();
        serviceCollection.AddSingleton<CreateProductCommandHandler>();
        
        Services = serviceCollection.BuildServiceProvider();
    }
}