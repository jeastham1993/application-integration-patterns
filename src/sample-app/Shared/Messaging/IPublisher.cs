namespace Shared.Messaging;

public interface IPublisher
{
    Task Publish<TMessageType>(string publishTo, MessageWrapper<TMessageType> message);
}