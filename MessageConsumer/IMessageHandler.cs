using MessageShared;

namespace MessageConsumer;

public interface IMessageHandler
{
    MessageHandlingResult HandleMessage(Message message);
}