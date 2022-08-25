using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace SynchronousApi.Implementations;

public static class Metrics
{
    private static AmazonCloudWatchClient _client;

    static Metrics()
    {
        _client = new AmazonCloudWatchClient();
    }

    public static async Task IncrementMetric(string metric, double value)
    {
        var request = new PutMetricDataRequest
        {
            MetricData = new List<MetricDatum>
            {
                new MetricDatum
                {
                    MetricName = metric,
                    Value = value,
                    Unit = StandardUnit.Count
                }
            },
            Namespace = "DotnetLambdaSample"
        };

        await _client.PutMetricDataAsync(request);
    }

}