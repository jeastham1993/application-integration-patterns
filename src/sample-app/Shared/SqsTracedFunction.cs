using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared.Messaging;

namespace Shared;

public abstract class SqsTracedFunction<TResponse> : BatchTracedFunction<SQSEvent, TResponse, SQSEvent.SQSMessage>
{
    public override Func<SQSEvent.SQSMessage, Activity, bool> AddRequestAttributes => SqsRequestAttributeLoader;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => SqsResponseAttributeLoader;

    public override Func<SQSEvent.SQSMessage, ActivityContext> HydrateMessageWithContext =>
        HydrateContextFromSnsMessage;
    
    public override Func<SQSEvent, ILambdaContext, Task<TResponse>> Handler => FunctionHandler;

    private bool SqsPropogator(SQSEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();
        return true;
    }
    
    private bool SqsRequestAttributeLoader(SQSEvent.SQSMessage arg, Activity activity)
    {
        activity.AddTag("faas.trigger", "pubsub");
        activity.AddTag("messaging.operation", "process");
        activity.AddTag("messaging.system", "AmazonSQS");
        activity.AddTag("messaging.destination_kind", "queue");

        return true;
    }
    
    private bool SqsResponseAttributeLoader(TResponse arg, Activity activity)
    {
        return true;
    }

    public async Task<string> FunctionHandler(SQSEvent evt, ILambdaContext context)
    {
        foreach (var record in evt.Records)
        {
            await this.HandleMessage(record, context);
        }

        return "OK";
    }

    public ActivityContext HydrateContextFromSnsMessage(SQSEvent.SQSMessage message)
    {
        this.ActivitySource = new ActivitySource(SERVICE_NAME);
        
        var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(message.Body);
        var wrappedMessage = JsonSerializer.Deserialize<MessageWrapper<dynamic>>(snsData.Message);
        
        var hydratedContext = new ActivityContext(ActivityTraceId.CreateFromString(wrappedMessage.Metadata.TraceParent.AsSpan()),
            ActivitySpanId.CreateFromString(wrappedMessage.Metadata.ParentSpan.AsSpan()), ActivityTraceFlags.Recorded);

        return hydratedContext;
    }
}