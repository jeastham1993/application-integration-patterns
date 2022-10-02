using System.Diagnostics;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Honeycomb.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Honeycomb.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Contrib.Instrumentation.AWSLambda.Implementation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared;

public abstract class TracedFunction<TRequestType, TResponseType>
{
    private TracerProvider _tracerProvider;
    public ActivityContext Context;
    private ActivitySource source;

    public abstract string SERVICE_NAME { get; }

    public abstract Func<TRequestType, ILambdaContext, Task<TResponseType>> Handler { get; }

    public abstract Func<TRequestType, ILambdaContext, bool> ContextPropagator { get; }
    
    public abstract Func<TRequestType, Activity, bool> AddRequestAttributes { get; }
    
    public abstract Func<TResponseType, Activity, bool> AddResponseAttributes { get; }

    public TracedFunction()
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

    public TracedFunction(TracerProvider provider)
    {
        _tracerProvider = provider;
    }

    public async Task<TResponseType> TracedFunctionHandler(TRequestType request,
        ILambdaContext context)
    {
        this.ContextPropagator.Invoke(request, context);

        if (Activity.Current == null)
        {
            source = new ActivitySource(SERVICE_NAME);
        }

        using (var rootSpan = (Activity.Current == null ? source : Activity.Current.Source).StartActivity(context.FunctionName, ActivityKind.Server, parentContext: this.Context))
        {
            this.AddRequestAttributes(request, rootSpan);
            
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
                Func<Task> action = async () => result = await Handler(request, context);

                await action();

                this.AddResponseAttributes(result, rootSpan);

                return result;
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
}