namespace SNSPublisher;

public class ProductCreatedEvent : EventBase
{
    public override string EventName => "product-created";
}