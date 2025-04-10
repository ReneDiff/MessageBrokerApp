using MessageShared;

namespace MessageConsumer.interfaces;

public interface IMessageHandler
{
    MessageHandlingResult HandleMessage(Message message);
}