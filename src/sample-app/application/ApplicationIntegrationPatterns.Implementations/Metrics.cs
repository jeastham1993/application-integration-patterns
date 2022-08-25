using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Metrics;

namespace ApplicationIntegrationPatterns.Implementations;

public static class MetricService
{
    static MetricService()
    {
    }

    public static void IncrementMetric(string metric, double value)
    {
        if (AWSXRayRecorder.IsLambda())
        {
            Metrics.AddMetric(metric, value);
        }
    }

}