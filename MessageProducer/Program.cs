using MessageShared;
using MessageProducer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Registrer RabbitMqProducer. Singleton er fint her, da den holder en forbindelse.
                // DI containeren vil kalde Dispose automatisk hvis den implementerer IDisposable.
                services.AddSingleton<RabbitMqProducer>();

                // Registrer din worker service
                services.AddHostedService<ProducerWorker>();
            });
}
