using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Infrastructure.Persistence;

namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Outbox writer implementing IOutbox by delegating to the ambient ApplicationDbContext.
/// </summary>
public sealed class OutboxWriter : IOutbox
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxWriter(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        _dbContext.Enqueue(integrationEvent);
    }
}
