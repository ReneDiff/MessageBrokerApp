using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MessageShared;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging; 
using Microsoft.Extensions.Configuration; // Tilføj denne


namespace MessageProducer;

// Ansvarlig for at sende beskeder til RabbitMQ.

public class RabbitMqProducer : IDisposable // Implementer IDisposable
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "Message-Queue";

    // Inject IConfiguration og ILogger
    public RabbitMqProducer(IConfiguration configuration, ILogger<RabbitMqProducer> logger)
    {
            _logger = logger; // Gem loggeren

            // Hent RabbitMQ hostname fra konfigurationen
            // Nøglen "RabbitMq:HostName" matcher strukturen i appsettings.json
            // Vi bruger ?? "localhost" som en sikkerheds-fallback, hvis nøglen mangler
            string hostname = configuration["RabbitMq:HostName"] ?? "localhost";

            _logger.LogInformation("Producer connecting to RabbitMQ Host: {HostName}", hostname);

            try
            {
                // Brug hostname fra konfigurationen her
                var factory = new ConnectionFactory() { HostName = hostname };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // QueueDeclare er uændret, men overvej durable: true senere
                _channel.QueueDeclare(queue: QueueName,
                                      durable: false,
                                      exclusive: false,
                                      autoDelete: false,
                                      arguments: null);
                _logger.LogInformation("RabbitMQ connection established and queue '{QueueName}' declared for Producer.", QueueName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect Producer to RabbitMQ host {HostName}", hostname);
                throw; // Vigtigt at kaste fejlen, så applikationen ved, at opstart fejlede
            }
    }

    //Sender besked til køen
    public void Send(Message message)
    {
            if (!_channel.IsOpen)
            {
                _logger.LogError("Cannot send message, RabbitMQ channel is not open.");
                // Overvej mere robust fejlhåndtering her
                return;
            }

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            try
            {
                // Overvej basicProperties for persistens senere
                _channel.BasicPublish(exchange: "",
                                    routingKey: QueueName,
                                    basicProperties: null,
                                    body: body);

                // Brug Debug level her, da det sker ofte
                _logger.LogDebug("[Producer] Sent: Counter={Counter}, Time={Timestamp:HH:mm:ss}", message.Counter, message.Timestamp);
                // Fjern eller behold Console.WriteLine efter behov
                // Console.WriteLine($"[Producer] Sent: Counter={message.Counter}, Time={message.Timestamp:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message. Counter: {Counter}", message.Counter);
            }
    }

    // Rydder pænt op i forbindelser/kanaler
    public void Dispose()
    {
            _logger.LogInformation("Disposing RabbitMqProducer...");
            try
            {
                // Prøv at lukke kanalen først
                _channel?.Close();
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ channel."); // Brug Warning, da det sker under nedlukning
            }
            try
            {
                // Luk derefter forbindelsen
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ connection.");
            }
             _logger.LogInformation("RabbitMqProducer disposed.");
    }
}