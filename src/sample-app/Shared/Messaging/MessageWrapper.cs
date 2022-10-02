using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Shared.Messaging;

public record MessageWrapper<T>()
{
    public MessageMetadata Metadata { get; set; } = new MessageMetadata();

    public T Data { get; set; }
}

public record MessageMetadata
{
    public MessageMetadata()
    {
        this.TraceParent = Activity.Current.TraceId.ToString();
        this.ParentSpan = Activity.Current.SpanId.ToString();
    }
    
    [JsonPropertyName("traceparent")]
    public string TraceParent { get; set; }
    
    [JsonPropertyName("parentspan")]
    public string ParentSpan { get; set; }
}