using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Default IIntegrationEventPublisher adapter that emits structured logs for published integration events.
/// In production, this adapter is swapped for a concrete message broker publisher (e.g. RabbitMQ, Kafka, Azure Service Bus).
/// </summary>
public sealed partial class LoggingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ILogger<LoggingIntegrationEventPublisher> _logger;

    public LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        LogEventPublished(_logger, integrationEvent.ContractName, integrationEvent.MessageId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published integration event '{ContractName}' (MessageId: {MessageId}).")]
    private static partial void LogEventPublished(ILogger logger, string contractName, Guid messageId);
}
