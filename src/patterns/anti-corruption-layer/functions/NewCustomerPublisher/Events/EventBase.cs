using System;

namespace NewCustomerPublisher.Events;

public abstract class EventBase
{
    public abstract string EventName { get; }

    public string Source => $"/product-service/{Environment.GetEnvironmentVariable("dev")}";
}