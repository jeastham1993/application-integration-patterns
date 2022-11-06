namespace NewCustomerPublisher.Events;

public class NewCustomerCreatedEvent : EventBase, IEventBase
{
    public override string EventName => "customer-created";
    
    public string CustomerId { get; set; }
}