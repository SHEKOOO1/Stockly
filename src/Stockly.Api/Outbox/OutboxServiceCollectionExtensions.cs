using Microsoft.Extensions.Configuration;
using Stockly.Infrastructure.Outbox;

namespace Stockly.Api.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(OutboxOptions.SectionName).Get<OutboxOptions>() ?? new OutboxOptions();
        options.Validate();

        var monitorConnection = configuration.GetConnectionString(OutboxOptions.MonitorConnectionName)
            ?? Environment.GetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable);
        var workerConnection = configuration.GetConnectionString(OutboxOptions.WorkerConnectionName)
            ?? Environment.GetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable);

        services.AddSingleton(options);
        services.AddSingleton(new OutboxMonitor(monitorConnection, options));
        services.AddSingleton(new OutboxCoordinator(workerConnection, options));
        services.AddSingleton<OutboxDispatcher>();
        services.AddHostedService(provider => provider.GetRequiredService<OutboxDispatcher>());

        return services;
    }
}
