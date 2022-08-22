using Amazon.SimpleNotificationService;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SNSPublisher;

public class EventProducer
{
    private readonly AmazonSimpleNotificationServiceClient _snsClient;

    public EventProducer(AmazonSimpleNotificationServiceClient snsClient)
    {
        this._snsClient = snsClient;
    }

    public async Task Publish(EventBase evt)
    {
        await this._snsClient.PublishAsync(Environment.GetEnvironmentVariable("PRODUCT_CREATED_TOPIC_ARN"), JsonSerializer.Serialize(evt));
    }
}