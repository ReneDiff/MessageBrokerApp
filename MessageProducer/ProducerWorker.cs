using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Vi tilføjer logging senere, men det er godt at have med
using MessageShared;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data;

namespace MessageProducer;

public class ProducerWorker : BackgroundService // BackgroundService er en nem base klasse
{
    private readonly ILogger<ProducerWorker> _logger;
    private readonly RabbitMqProducer _producer; // injiceret service

    public ProducerWorker(ILogger<ProducerWorker> logger, RabbitMqProducer producer)
    {
        _logger = logger;
        _producer = producer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProducerWorker Starter."); // Eksempel på logging
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = new Message
            {
                Timestamp = DateTime.UtcNow,
                Counter = counter++
            };

            try
            {
                _producer.Send(message);
                // Log evt. succes her med _logger
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ved at sende besked. Counter: {Counter}", message.Counter);
                // Overvej en kort pause ved fejl for ikke at spamme loggen/køen
                await Task.Delay(5000, stoppingToken);
            }
            await Task.Delay(1000, stoppingToken); // Brug stoppingToken her også
        }
        _logger.LogInformation("ProducerWorker stopping.");
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProducerWorker stopping. Disposing producer...");
        _producer.Dispose(); // Sørg for at lukke forbindelsen pænt
        await base.StopAsync(cancellationToken);
    }
}