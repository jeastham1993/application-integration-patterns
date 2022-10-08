using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.DynamoDBEvents;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared.Messaging;

namespace Shared;

public abstract class DynamoDbStreamTracedFunction<TResponse> : TracedFunction<DynamoDBEvent, TResponse>
{
    public override Func<DynamoDBEvent, ILambdaContext, bool> ContextPropagator => DynamoDbPropagator;
    
    public override Func<DynamoDBEvent, Activity, bool> AddRequestAttributes => DynamoDbRequestAttributeLoader;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => DynamoDbResponseAttributeLoader;

    private bool DynamoDbPropagator(DynamoDBEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();
        return true;
    }
    
    private bool DynamoDbRequestAttributeLoader(DynamoDBEvent arg, Activity activity)
    {
        activity.AddTag("faas.trigger", "stream");
        activity.AddTag("messaging.operation", "process");
        activity.AddTag("messaging.system", "DynamoDB");
        activity.AddTag("messaging.destination_kind", "stream");

        return true;
    }
    
    private bool DynamoDbResponseAttributeLoader(TResponse arg, Activity activity)
    {
        return true;
    }

    public ActivityContext HydrateContextFromStreamRecord(DynamoDBEvent.DynamodbStreamRecord message)
    {
        if (!message.Dynamodb.NewImage.ContainsKey("TraceParent"))
        {
            return new ActivityContext();
        }

        var hydratedContext = new ActivityContext(ActivityTraceId.CreateFromString(message.Dynamodb.NewImage["TraceParent"].S),
            ActivitySpanId.CreateFromString(message.Dynamodb.NewImage["ParentSpan"].S.AsSpan()), ActivityTraceFlags.Recorded);

        return hydratedContext;
    }
}