using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.DynamoDBEvents;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared.Messaging;

namespace Shared;

public abstract class DynamoDbStreamTracedFunction<TResponse> : BatchTracedFunction<DynamoDBEvent, TResponse, DynamoDBEvent.DynamodbStreamRecord>
{
    public override Func<DynamoDBEvent.DynamodbStreamRecord, Activity, bool> AddRequestAttributes => DynamoDbRequestAttributeLoader;
    
    public override Func<DynamoDBEvent, ILambdaContext, Task<TResponse>> Handler => FunctionHandler;
    
    public override Func<TResponse, Activity, bool> AddResponseAttributes => DynamoDbResponseAttributeLoader;

    public override Func<DynamoDBEvent.DynamodbStreamRecord, ActivityContext> HydrateMessageWithContext => HydrateContextFromStreamRecord;

    private bool DynamoDbPropagator(DynamoDBEvent arg, ILambdaContext context)
    {
        this.Context = new ActivityContext();
        return true;
    }
    
    private bool DynamoDbRequestAttributeLoader(DynamoDBEvent.DynamodbStreamRecord arg, Activity activity)
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

    public async Task<string> FunctionHandler(DynamoDBEvent dynamoStreamEvent, ILambdaContext context)
    {
        foreach (var evt in dynamoStreamEvent.Records)
        {
            await this.HandleMessage(evt, context);
        }

        return "OK";
    }
}