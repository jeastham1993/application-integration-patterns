namespace SNSSubscriber.Events;

public class ProductCreatedEvent : EventBase
{
    public override string EventName => "customer-created";
}