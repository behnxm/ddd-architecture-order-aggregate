using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Infrastructure.Catalog;
using DddOrderAggregate.Infrastructure.Interceptors;
using DddOrderAggregate.Infrastructure.Outbox;
using DddOrderAggregate.Infrastructure.Persistence;
using DddOrderAggregate.Infrastructure.ReadModels;
using DddOrderAggregate.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DddOrderAggregate.Infrastructure;

/// <summary>
/// Composition helper registering infrastructure persistence, outbox services, and ACL adapters.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // System time provider
        services.AddSingleton(TimeProvider.System);

        // Interceptor for atomic domain event dispatch before SaveChanges commits
        services.AddScoped<DispatchDomainEventsInterceptor>();

        // DbContext and Unit of Work registration
        var connectionString = configuration.GetConnectionString("Database") ?? "Data Source=orders.db";
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();
            options.UseSqlite(connectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IOutbox, OutboxWriter>();

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();

        // Catalog Anti-Corruption Adapter
        services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();

        // Outbox Infrastructure
        services.Configure<OutboxOptions>(options =>
        {
            var section = configuration.GetSection(OutboxOptions.SectionName);
            if (int.TryParse(section[nameof(OutboxOptions.IntervalMilliseconds)], out var interval))
            {
                options.IntervalMilliseconds = interval;
            }
            if (int.TryParse(section[nameof(OutboxOptions.BatchSize)], out var batchSize))
            {
                options.BatchSize = batchSize;
            }
            if (int.TryParse(section[nameof(OutboxOptions.MaxAttempts)], out var maxAttempts))
            {
                options.MaxAttempts = maxAttempts;
            }
        });

        services.AddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
        services.AddHostedService<OutboxProcessor>();

        // EF Core Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: ["ready"]);

        return services;
    }
}
