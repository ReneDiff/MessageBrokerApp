using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MessageConsumer; // Tilføj dit namespace
using Microsoft.Extensions.Logging; // Tilføj denne for at logge
using System; // Tilføjet for InvalidOperationException


public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Log det aktive miljø for at bekræfte
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var env = host.Services.GetRequiredService<IHostEnvironment>();
        logger.LogWarning("##### MessageConsumer starter i miljø: {EnvironmentName} #####", env.EnvironmentName);

        await host.RunAsync();
        // await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        // ConfigureAppConfiguration kan bruges til mere avanceret konfigurationsopsætning,
        // men CreateDefaultBuilder håndterer appsettings*.json og miljøvariabler automatisk.
        .ConfigureServices((hostContext, services) => // hostContext har adgang til IConfiguration
        {
            // Registrer services
            services.AddSingleton<IMessageHandler, MessageHandler>();

            // Hent IConfiguration fra hostContext (bygget af CreateDefaultBuilder)
            IConfiguration configuration = hostContext.Configuration;

            // Brug GetConnectionString til at hente strengen defineret under "ConnectionStrings"
            // Navnet "PostgresDb" skal matche nøglen i appsettings.json
            string? connectionString = configuration.GetConnectionString("PostgresDb");

            // Tjek om connection string blev fundet - ellers kan appen ikke køre
            if (string.IsNullOrEmpty(connectionString))
            {
                // Log evt. en kritisk fejl her via en midlertidig logger, eller smid exception
                throw new InvalidOperationException("Database connection string 'PostgresDb' not found or is empty in configuration.");
            }

            // Registrer PostgresService og giv den connection string med
            // Nu er den ikke længere hardkodet her
            services.AddSingleton<IDatabaseService>(sp =>
                new PostgresService(connectionString));

            // Registrer RabbitMqConsumer som Singleton
            // DI containeren vil automatisk injecte IMessageHandler, IDatabaseService,
            // ILogger<RabbitMqConsumer> og IConfiguration baseret på dens konstruktør.
            services.AddSingleton<RabbitMqConsumer>();

            // Registrer din worker service (uændret)
            services.AddHostedService<ConsumerWorker>();
        });
}