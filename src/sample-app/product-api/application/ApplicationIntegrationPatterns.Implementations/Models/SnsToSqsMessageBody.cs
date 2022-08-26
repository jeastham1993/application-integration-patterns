using Amazon.SimpleNotificationService.Model;
namespace ApplicationIntegrationPatterns.Implementations.Models;

public record SnsToSqsMessageBody
{
    public string MessageId { get; set; }

    public string TopicArn { get; set; }

    public string Message { get; set; }

    public Dictionary<string, StringMessageAttribute> MessageAttributes { get; set; }
}

public record StringMessageAttribute
{
    public string Type { get; set; }

    public string Value { get; set; }
}

