using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared.Messaging;

namespace Shared;

public abstract class SqsTracedFunction<TResponse> : TracedFunction<SQSEvent, TResponse>
{
    public override Func<SQSEvent, ILambdaContext, bool> ContextPropagator => SqsPropogator;
    
    public override Func<SQSEvent, Activity, bool> AddRequestAttributes => SqsRequestAttributeLoader;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => SqsResponseAttributeLoader;

    private bool SqsPropogator(SQSEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();
        return true;
    }
    
    private bool SqsRequestAttributeLoader(SQSEvent arg, Activity activity)
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

    public ActivityContext HydrateContextFromSnsMessage(SQSEvent.SQSMessage message)
    {
        var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(message.Body);
        var wrappedMessage = JsonSerializer.Deserialize<MessageWrapper<dynamic>>(snsData.Message);
        
        var hydratedContext = new ActivityContext(ActivityTraceId.CreateFromString(wrappedMessage.Metadata.TraceParent.AsSpan()),
            ActivitySpanId.CreateFromString(wrappedMessage.Metadata.ParentSpan.AsSpan()), ActivityTraceFlags.Recorded);

        return hydratedContext;
    }
}