namespace EventBridgePublisher;

public class ProductCreatedEvent : EventBase
{
    public override string EventName => "product-created";
}