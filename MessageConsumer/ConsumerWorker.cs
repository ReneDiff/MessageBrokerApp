// MessageConsumer/ConsumerWorker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageConsumer
{
    public class ConsumerWorker : IHostedService // Implementer IHostedService direkte for mere kontrol
    {
        private readonly ILogger<ConsumerWorker> _logger;
        private readonly RabbitMqConsumer _consumer; // Injiceret service

        public ConsumerWorker(ILogger<ConsumerWorker> logger, RabbitMqConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
             _logger.LogInformation("ConsumerWorker starting.");
             try
             {
                _consumer.StartConsuming();
             }
             catch (Exception ex)
             {
                // Log at start fejlede kritisk
                _logger.LogCritical(ex, "Failed to start RabbitMqConsumer in ConsumerWorker.");
                // Propagate fejlen for at stoppe hosten hvis nødvendigt
                return Task.FromException(ex);
             }
             return Task.CompletedTask; // StartConsuming kører i baggrunden pga. event handler
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
             _logger.LogInformation("ConsumerWorker stopping.");
             _consumer.Dispose(); // Kald Dispose på consumeren for at rydde op
             return Task.CompletedTask;
        }
    }
}