using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using MessageShared;
using Microsoft.Extensions.Logging; // Tilføj logging
using System; // Tilføj for IDisposable
using Microsoft.Extensions.Configuration; // Tilføjet for konfiguration


namespace MessageConsumer;

// Lytter på RabbitMQ og håndterer beskeder via MessageHandler og DB.

public class RabbitMqConsumer :IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection; // Gem forbindelsen for at kunne dispose
    private readonly IMessageHandler _handler;
    private readonly IDatabaseService _database;
    private readonly ILogger<RabbitMqConsumer> _logger; // Tilføj logger
    private const string QueueName = "Message-Queue";
    private string? _consumerTag; // Til at stoppe consumeren

    public RabbitMqConsumer(IMessageHandler handler, IDatabaseService database, ILogger<RabbitMqConsumer> logger, IConfiguration configuration)
    {
        _handler = handler;
        _database = database;
        _logger = logger; // Gem logger
        // _configuration = configuration; // Gem evt. configuration hvis den skal bruges andre steder

        // Læs hostname fra konfiguration
        string hostname = configuration["RabbitMq:HostName"] ?? "localhost"; // Brug fallback
        _logger.LogInformation("Consumer connecting to RabbitMQ Host: {HostName}", hostname);

        try
        {
            // Brug hostname fra konfigurationen her
            var factory = new ConnectionFactory() { HostName = hostname };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // QueueDeclare uændret, men overvej durable: true senere
            _channel.QueueDeclare(queue: QueueName,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
            _logger.LogInformation("RabbitMQ connection and channel established. Queue '{QueueName}' declared.", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to connect Consumer to RabbitMQ host {HostName}", hostname);
            throw;
        }
    }

    public void StartConsuming() 
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogError("Cannot start consuming, channel is not available or closed.");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) => // Gør lambda async for await Task.Delay
        {
            var body = ea.Body.ToArray();
            Message? message = null;
            string messageBodyForLogging = string.Empty; // Til logning ved fejl

            try
            {
                // Prøv at læse body som string *før* deserialisering for bedre fejl-logning
                messageBodyForLogging = Encoding.UTF8.GetString(body);
                message = JsonSerializer.Deserialize<Message>(body);

                // *** VIGTIGT: Tjek om beskeden blev deserialiseret korrekt ***
                if (message == null)
                {
                    _logger.LogError("Failed to deserialize message body to Message object. Body: {BodyString}", messageBodyForLogging);
                    _channel.BasicAck(ea.DeliveryTag, false); // Ack for at fjerne den ugyldige besked
                    return; // Stop behandling af denne besked
                }

                // Kald MessageHandler for at bestemme handling
                var handlingResult = _handler.HandleMessage(message);

                // Udfør handling baseret på resultatet
                switch (handlingResult)
                {
                    case MessageHandlingResult.SaveToDatabase:
                        _logger.LogInformation("Attempting to save message Counter={Counter}", message.Counter);
                        try
                        {
                            await _database.SaveMessageAsync(message);
                            _logger.LogInformation("Message saved successfully. Counter={Counter}. Acking message.", message.Counter);
                            _channel.BasicAck(ea.DeliveryTag, false); // Bekræft modtagelse
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogError(dbEx, "Error saving message Counter={Counter} to database. Nacking message (no requeue).", message.Counter);
                            // Nack uden requeue for at undgå gentagne DB fejl
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                        break;

                    case MessageHandlingResult.RequeueWithIncrement:
                        message.Counter++; // Forøg tæller
                        _logger.LogInformation("Message needs requeue. Incrementing counter to {Counter}. Preparing to requeue after delay.", message.Counter);

                        try
                        {
                            // Indsæt delay før requeue
                            await Task.Delay(1000); // Vent 1 sekund
                            _logger.LogDebug("Delay before requeue finished for message Counter={Counter}", message.Counter);

                            var requeuedBody = JsonSerializer.SerializeToUtf8Bytes(message);
                            // Publicer den *modificerede* besked tilbage til køen
                            _channel.BasicPublish(exchange: "",
                                                routingKey: QueueName,
                                                basicProperties: null, // Overvej persistens her også
                                                body: requeuedBody);
                            _logger.LogInformation("Message requeued successfully with Counter={Counter}. Acking original message.", message.Counter);
                            _channel.BasicAck(ea.DeliveryTag, false); // Ack den *originale* besked, da vi har genpubliceret den
                        }
                        catch (Exception reqEx)
                        {
                            _logger.LogError(reqEx, "Error during requeue process for message Counter={Counter}. Nacking original message (no requeue).", message.Counter);
                            // Fejl under delay eller republishing - Nack den originale uden requeue
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                        break;

                    case MessageHandlingResult.Discard:
                    default: // Inkluderer Discard
                        _logger.LogInformation("Discarding message Counter={Counter} based on handler result. Acking message.", message.Counter);
                        _channel.BasicAck(ea.DeliveryTag, false); // Bekræft (fjern) beskeden
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON Deserialization error. Discarding message. Body: {BodyString}", messageBodyForLogging); // Brug gemt body string
                _channel.BasicAck(ea.DeliveryTag, false); // Fjern den ugyldige besked
            }
            catch (Exception ex)
            {
                // Log counter hvis message blev delvist deserialiseret, ellers -1
                int counterForLog = message?.Counter ?? -1;
                _logger.LogError(ex, "Unexpected error processing message: Counter={Counter}. Nacking message (no requeue). Body: {BodyString}", counterForLog, messageBodyForLogging);
                _channel.BasicNack(ea.DeliveryTag, false, false); // Undgå loops ved uventede fejl
            }
        };

        // Start consumeren
        _consumerTag = _channel.BasicConsume(queue: QueueName,
                                autoAck: false, // Manuel acknowledgement er VIGTIGT
                                consumer: consumer);

        _logger.LogInformation("[Consumer] Listening for messages on queue '{QueueName}'...", QueueName);
    }

    public void StopConsuming()
    {
        if (_consumerTag != null && _channel != null && _channel.IsOpen)
        {
            _logger.LogInformation("Stopping consumer with tag: {ConsumerTag}", _consumerTag);
            try
            {
                _channel.BasicCancel(_consumerTag);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Exception during BasicCancel for consumer tag {ConsumerTag}", _consumerTag);
            }
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing RabbitMqConsumer...");
        try
        {
            StopConsuming(); // Sørg for at stoppe consumer før lukning
            _channel?.Close();
            _channel?.Dispose();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Error disposing RabbitMQ channel."); }
        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Error disposing RabbitMQ connection."); }
        _logger.LogInformation("RabbitMqConsumer disposed.");
    }
}