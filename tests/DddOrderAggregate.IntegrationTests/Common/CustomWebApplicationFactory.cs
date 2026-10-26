using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DddOrderAggregate.IntegrationTests.Common;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly List<IIntegrationEvent> _publishedEvents = [];

    public IReadOnlyList<IIntegrationEvent> PublishedEvents => _publishedEvents.AsReadOnly();

    public CustomWebApplicationFactory()
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True;");
        _sqliteConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Register DbContext with the open in-memory SQLite connection
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite(_sqliteConnection);
            });

            // Replace publisher with test spy
            var publisherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IIntegrationEventPublisher));
            if (publisherDescriptor is not null)
            {
                services.Remove(publisherDescriptor);
            }

            services.AddSingleton<IIntegrationEventPublisher>(new TestIntegrationEventPublisher(_publishedEvents));
        });
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _sqliteConnection.Dispose();
        }
    }

    private sealed class TestIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly List<IIntegrationEvent> _events;

        public TestIntegrationEventPublisher(List<IIntegrationEvent> events)
        {
            _events = events;
        }

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            lock (_events)
            {
                _events.Add(integrationEvent);
            }
            return Task.CompletedTask;
        }
    }
}
