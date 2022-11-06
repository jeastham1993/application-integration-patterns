using System;

namespace NewCustomerPublisher.Events;

public interface IEventBase
{
    string EventName { get; }

    string Source => $"/product-service/{Environment.GetEnvironmentVariable("dev")}";
}