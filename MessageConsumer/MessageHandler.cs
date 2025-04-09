using MessageShared;
using Microsoft.Extensions.Logging;

namespace MessageConsumer;

public class MessageHandler : IMessageHandler
{
    private const int MaxRetries = 5;
    private readonly ILogger<MessageHandler> _logger;

    // Inject ILogger via konstruktør
    public MessageHandler(ILogger<MessageHandler> logger)
    {
        _logger = logger;
        _logger.LogWarning(">>> MessageHandler INSTANCE CREATED! Logger is available: {IsLoggerAvailable}", _logger != null);
    }
    public MessageHandlingResult HandleMessage(Message message)
    {
        var now = DateTime.UtcNow;
        var age = now - message.Timestamp;
        int second = message.Timestamp.Second; // Gem sekund-værdi for logning
        int counter = message.Counter; // Gem counter for logning

        _logger.LogDebug("Handling message. Counter={Counter}, Timestamp={Timestamp}, Second={Second}, Age={Age}",
            counter, message.Timestamp, second, age);
            
            // 1. Tjek for gammel besked
        if (age > TimeSpan.FromMinutes(1))
        {
            _logger.LogInformation("MessageHandler: Message is too old ({Age}). Returning Discard. Counter={Counter}", age, counter);
            return MessageHandlingResult.Discard;
        }

            // 2. Tjek for lige sekund -> Gem
        if (message.Timestamp.Second % 2 == 0)
        {
            _logger.LogInformation("MessageHandler: Timestamp second ({Second}) is even. Returning SaveToDatabase. Counter={Counter}", second, counter);
            return MessageHandlingResult.SaveToDatabase;
        }

            // 3. Tjek for Max Retries -> Kasser
        if (message.Counter >= MaxRetries)
        {
            _logger.LogWarning("MessageHandler: Max retries ({MaxRetries}) reached. Returning Discard. Counter={Counter}", MaxRetries, counter);
            return MessageHandlingResult.Discard;
        }

            // 4. Ellers -> Requeue
        _logger.LogInformation("MessageHandler: Timestamp second ({Second}) is odd and retries not exceeded. Returning RequeueWithIncrement. Counter={Counter}", second, counter);
        return MessageHandlingResult.RequeueWithIncrement;
    }
}