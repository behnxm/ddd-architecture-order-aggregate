using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Events;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Application.Orders.EventHandlers;

/// <summary>
/// Translates internal OrderSubmittedDomainEvent into an asynchronous OrderSubmittedIntegrationEvent and writes it to the Outbox.
/// </summary>
public sealed partial class OrderSubmittedDomainEventHandler : IDomainEventHandler<OrderSubmittedDomainEvent>
{
    private readonly IOutbox _outbox;
    private readonly ILogger<OrderSubmittedDomainEventHandler> _logger;

    public OrderSubmittedDomainEventHandler(IOutbox outbox, ILogger<OrderSubmittedDomainEventHandler> logger)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public Task HandleAsync(OrderSubmittedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrderSubmitted(_logger, domainEvent.OrderId.Value);

        var integrationEvent = OrderSubmittedIntegrationEvent.Create(
            domainEvent.OrderId.Value,
            domainEvent.CustomerId.Value,
            domainEvent.Total.Amount,
            domainEvent.Total.Currency.Code,
            domainEvent.OccurredOn);

        _outbox.Enqueue(integrationEvent);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Domain event OrderSubmitted received for Order '{OrderId}'. Enqueuing integration event.")]
    private static partial void LogOrderSubmitted(ILogger logger, Guid orderId);
}
