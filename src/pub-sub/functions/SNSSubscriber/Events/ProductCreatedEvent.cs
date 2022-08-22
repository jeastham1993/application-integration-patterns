namespace SNSSubscriber;

public class ProductCreatedEvent : EventBase
{
    public override string EventName => "product-created";
}