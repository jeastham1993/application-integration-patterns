using ApplicationIntegrationPatterns.Core.DataTransfer;

namespace ApplicationIntegrationPatterns.Core.Events
{
    public record ProductCreatedEvent
    {
        public string EventName => "product-created";

        public ProductDTO Product { get; init; }
    }
}
