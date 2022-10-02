using System.Diagnostics;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Shared.Messaging;

public class SqsQueuing : IQueuing
{
    private readonly IAmazonSQS _sqsClient;

    public SqsQueuing(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
    }
    
    public async Task Enqueue<TMessageType>(string queueUrl, MessageWrapper<TMessageType> message)
    {
        using var activity = Activity.Current?.Source.StartActivity("SQSSendMessage");
        var (filepath, lineno, function) = TraceUtils.CodeInfo();
        activity?.AddTag("code.function", function);
        activity?.AddTag("code.lineno", lineno - 2);
        activity?.AddTag("code.filepath", filepath);
        activity.AddTag("messaging.contents", JsonSerializer.Serialize(message.Data));
        
        await this._sqsClient.SendMessageAsync(new SendMessageRequest()
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>(1)
            {
                {
                    "parentspan", new MessageAttributeValue()
                    {
                        StringValue = activity.SpanId.ToString(),
                        DataType = "String" 
                    }
                }
            }
        });
        
        activity.Stop();
    }
}