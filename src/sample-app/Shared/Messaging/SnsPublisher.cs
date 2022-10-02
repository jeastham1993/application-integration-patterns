using System.Diagnostics;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace Shared.Messaging;

public class SnsPublisher : IPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;

    public SnsPublisher(IAmazonSimpleNotificationService snsClient)
    {
        _snsClient = snsClient;
    }

    public async Task Publish<TMessageType>(string publishTo, MessageWrapper<TMessageType> message)
    {
        using var activity = Activity.Current?.Source.StartActivity("SNSPublish");
        var (filepath, lineno, function) = TraceUtils.CodeInfo();
        activity?.AddTag("code.function", function);
        activity?.AddTag("code.lineno", lineno - 2);
        activity?.AddTag("code.filepath", filepath);
        activity.AddTag("messaging.contents", JsonSerializer.Serialize(message.Data));
        
        await this._snsClient.PublishAsync(new PublishRequest()
        {
            TopicArn = publishTo,
            Message = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>(2)
            {
                {"traceparent", new MessageAttributeValue(){StringValue = activity.TraceId.ToString(), DataType = "String"}},
                {"parentspan", new MessageAttributeValue(){StringValue = activity.SpanId.ToString(), DataType = "String"}}
            }
        });
        
        activity.Stop();
    }
}