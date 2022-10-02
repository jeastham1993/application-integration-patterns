namespace Shared.Messaging;

public interface IQueuing
{
    Task Enqueue<TMessageType>(string queueUrl, MessageWrapper<TMessageType> message);
}