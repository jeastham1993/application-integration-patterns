using System.Diagnostics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Honeycomb.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared;

public abstract class BatchTracedFunction<TRequestType, TResponseType, TMessageType>
{
    private TracerProvider _tracerProvider;
    public ActivityContext Context;
    public ActivitySource ActivitySource;

    public abstract string SERVICE_NAME { get; }

    public abstract Func<TRequestType, ILambdaContext, Task<TResponseType>> Handler { get; }
    
    public abstract Func<TMessageType, ILambdaContext, Task> MessageProcessor { get; }
    
    public abstract Func<TMessageType, ActivityContext> HydrateMessageWithContext { get; }
    
    public abstract Func<TMessageType, Activity, bool> AddRequestAttributes { get; }
    
    public abstract Func<TResponseType, Activity, bool> AddResponseAttributes { get; }

    public BatchTracedFunction()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SERVICE_NAME))
            .AddSource(SERVICE_NAME)
            .AddAWSInstrumentation()
            .AddAWSLambdaConfigurations()
            .AddHoneycomb(new HoneycombOptions
            {
                ServiceName = SERVICE_NAME,
                ApiKey = Environment.GetEnvironmentVariable("HONEYCOMB_API_KEY"),
                EnableLocalVisualizations = true
            })
            .Build();
    }

    public BatchTracedFunction(TracerProvider provider)
    {
        _tracerProvider = provider;
    }

    public async Task<TResponseType> TracedFunctionHandler(TRequestType request,
        ILambdaContext context)
    {
            try
            {
                TResponseType result = default;
                Func<Task> action = async () => result = await Handler(request, context);

                await action();

                return result;
            }
            catch (Exception e)
            {
                if (Activity.Current == null)
                    throw;
                
                Activity.Current.SetStatus(Status.Error);
                Activity.Current.RecordException(e);

                this._tracerProvider.ForceFlush();

                Activity.Current.Stop();

                throw;
            }
            finally
            {
                if (Activity.Current != null)
                    Activity.Current.Stop();

                this._tracerProvider.ForceFlush();
            }
    }

    public async Task HandleMessage(TMessageType message, ILambdaContext context)
    {
        ActivitySource = new ActivitySource(SERVICE_NAME);
        
        this.Context = this.HydrateMessageWithContext.Invoke(message);

        using (var rootSpan = (Activity.Current == null ? ActivitySource : Activity.Current.Source).StartActivity(context.FunctionName, ActivityKind.Server, parentContext: this.Context))
        {
            rootSpan.AddTag("aws.lambda.invoked_arn", context.InvokedFunctionArn);
            rootSpan.AddTag("faas.id", context.InvokedFunctionArn);
            rootSpan.AddTag("faas.execution", context.AwsRequestId);
            rootSpan.AddTag("cloud.account.id", context.InvokedFunctionArn?.Split(":")[4]);
            rootSpan.AddTag("cloud.provider", "aws");
            rootSpan.AddTag("faas.name", context.FunctionName);
            rootSpan.AddTag("faas.version", context.FunctionVersion);

            try
            {
                using var handlerSpan = Activity.Current.Source.StartActivity($"{context.FunctionName}_Handler");

                TResponseType result = default;
                Func<Task> action = async () => await MessageProcessor(message, context);

                await action();

                this.AddResponseAttributes(result, rootSpan);

                return;
            }
            catch (Exception e)
            {
                rootSpan.SetStatus(Status.Error);
                rootSpan.RecordException(e);

                this._tracerProvider.ForceFlush();

                rootSpan.Stop();

                throw;
            }
            finally
            {
                rootSpan.Stop();

                this._tracerProvider.ForceFlush();
            }
        }
    }

    public void AddRootAttributesFrom(ILambdaContext context)
    {
        if (Activity.Current == null)
            return;
        
        Activity.Current.AddTag("aws.lambda.invoked_arn", context.InvokedFunctionArn);
        Activity.Current.AddTag("faas.id", context.InvokedFunctionArn);
        Activity.Current.AddTag("faas.execution", context.AwsRequestId);
        Activity.Current.AddTag("cloud.account.id", context.InvokedFunctionArn?.Split(":")[4]);
        Activity.Current.AddTag("cloud.provider", "aws");
        Activity.Current.AddTag("faas.name", context.FunctionName);
        Activity.Current.AddTag("faas.version", context.FunctionVersion);
    }
}