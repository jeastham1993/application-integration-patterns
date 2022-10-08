using System.Diagnostics;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;

namespace Shared;

public static class ActivityExtensions
{
    public static Activity AddSqsAttributes(this Activity span, SQSEvent.SQSMessage message)
    {
        span.AddTag("faas.trigger", "pubsub");
        span.AddTag("messaging.operation", "process");
        span.AddTag("messaging.system", "AmazonSQS");
        span.AddTag("messaging.destination_kind", "queue");

        return span;
    }
    public static Activity AddDynamoDbAttributes(this Activity span, DynamoDBEvent.DynamodbStreamRecord message)
    {
        span.AddTag("faas.trigger", "stream");
        span.AddTag("messaging.operation", "process");
        span.AddTag("messaging.system", "DynamoDB");
        span.AddTag("messaging.destination_kind", "stream");
        span.AddTag($"messaging.attribute.source-arn", message.EventSourceArn);
        span.AddTag($"messaging.attribute.event-name", message.EventName.Value);
        span.AddTag($"messaging.attribute.event-version", message.EventVersion);
        span.AddTag($"messaging.attribute.event-id", message.EventID);

        return span;
    }
    
    public static Activity AddSnsAttributes(this Activity span, SNSEvent.SNSMessage message)
    {
        span.AddTag("faas.trigger", "pubsub");
        span.AddTag("messaging.operation", "process");
        span.AddTag("messaging.system", "AmazonSNS");
        span.AddTag("messaging.destination_kind", "topic");

        foreach (var attr in message.MessageAttributes)
        {
            span.AddTag($"messaging.attribute.{attr.Key.ToLower().Replace("-", "_")}", attr.Value.Value);
        }

        return span;
    }
}