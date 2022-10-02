using System.Diagnostics;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Trace;

namespace Shared;

public abstract class ApiGatewayTracedFunction : TracedFunction<APIGatewayProxyRequest, APIGatewayProxyResponse>
{
    private const string OtelTraceHeaderKey = "traceparent";
    private const string AWSXRayLambdaTraceHeaderKey = "_X_AMZN_TRACE_ID";
    private const string AWSXRayTraceHeaderKey = "X-Amzn-Trace-Id";

    private static readonly Func<IDictionary<string, string>, string, IEnumerable<string>> Getter = (headers, name) =>
    {
        if (headers.TryGetValue(name, out var value))
        {
            return new[] {value};
        }

        return new string[0];
    };

    public override Func<APIGatewayProxyRequest, ILambdaContext, bool> ContextPropagator => ApiGatewayPropagator;

    public override Func<APIGatewayProxyRequest, Activity, bool> AddRequestAttributes => ApiGatewayAttributeLoader;

    public override Func<APIGatewayProxyResponse, Activity, bool> AddResponseAttributes =>
        ApiGatewayResponseAttributeLoader;

    public ApiGatewayTracedFunction(): base()
    {
        
    }

    public ApiGatewayTracedFunction(TracerProvider provider): base(provider)
    {
        
    }

    private bool ApiGatewayPropagator(APIGatewayProxyRequest arg, ILambdaContext context)
    {
        if (arg.Headers.ContainsKey(OtelTraceHeaderKey))
        {
            var traceID = arg.Headers[OtelTraceHeaderKey];

            this.Context = new ActivityContext(ActivityTraceId.CreateFromString(traceID.AsSpan()),
                ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        }
        else if (Environment.GetEnvironmentVariable("AWSXRayLambdaTraceHeaderKey") != null)
        {
            var propagator = new AWSXRayPropagator();

            var carrier = new Dictionary<string, string>()
            {
                {AWSXRayTraceHeaderKey, Environment.GetEnvironmentVariable(AWSXRayLambdaTraceHeaderKey)},
            };

            var propagationContext = propagator.Extract(default, carrier, Getter);

            this.Context = propagationContext.ActivityContext;
        }
        else
        {
            this.Context = default;
        }

        return true;
    }

    private bool ApiGatewayResponseAttributeLoader(APIGatewayProxyResponse arg, Activity activity)
    {
        foreach (var header in arg.Headers)
        {
            activity.AddTag($"http.response.header.{header.Key.ToLower().Replace("-", "_")}", header.Value);
        }

        return true;
    }

    private bool ApiGatewayAttributeLoader(APIGatewayProxyRequest arg, Activity activity)
    {
        activity.AddTag("faas.trigger", "http");
        activity.AddTag("http.method", arg.HttpMethod);
        activity.AddTag("http.user_agent", arg.Headers["User-Agent"]);
        if (arg.Headers.ContainsKey("Content-Length"))
        {
            activity.AddTag("http.request_content_length", arg.Headers["Content-Length"]);    
        }
        else
        {
            activity.AddTag("http.request_content_length", Encoding.UTF8.GetByteCount(arg.Body ?? "").ToString());
        }
        
        activity.AddTag("http.route", arg.Resource);
        activity.AddTag("http.target", arg.Path);

        if (arg.Headers.ContainsKey("x-forwarded-proto"))
        {
            activity.AddTag("http.scheme", arg.Headers["x-forwarded-proto"]);   
        }

        foreach (var header in arg.Headers)
        {
            if (header.Key == "User-Agent" || header.Key == "Content-Length" || header.Key == "x-forwarded-proto")
            {
                continue;
            }

            activity.AddTag($"http.request.header.{header.Key.ToLower().Replace("-", "_")}", header.Value);
        }

        return true;
    }
}