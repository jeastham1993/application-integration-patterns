using System;

namespace EventBridgePublisher;

public abstract class EventBase
{
    public abstract string EventName { get; }

    public string Source => $"/product-service/{Environment.GetEnvironmentVariable("dev")}";
}