using System.Diagnostics;
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

        foreach (var attr in message.Attributes)
        {
            span.AddTag($"messaging.attribute.{attr.Key.ToLower().Replace("-", "_")}", attr.Value);
        }

        foreach (var attr in message.Attributes)
        {
            var parsedAttribute = attr.Key.ToLower().Replace("-", "_");
            
            span.AddTag($"messaging.attribute.{attr.Key.ToLower().Replace("-", "_")}", attr.Value);
        }

        foreach (var attr in message.MessageAttributes.Where(p => p.Value.DataType == "String"))
        {
            var parsedAttribute = attr.Key.ToLower().Replace("-", "_");
            
            span.AddTag($"messaging.attribute.{parsedAttribute}", attr.Value.StringValue);
        }

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