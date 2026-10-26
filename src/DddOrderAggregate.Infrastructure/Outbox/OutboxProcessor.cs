using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Background worker that polls and dispatches pending transactional outbox messages.
/// 
/// Concurrency & Scale-Out Considerations:
/// - Single-Instance Guarantee: In this SQLite reference architecture, the outbox processor runs in a single process.
/// - Distributed Systems: Under PostgreSQL, competing consumers would coordinate via 'FOR UPDATE SKIP LOCKED'.
/// - Consumers must enforce idempotent deduplication keyed on IIntegrationEvent.MessageId.
/// </summary>
public sealed partial class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processes a batch of pending outbox messages. Exposed publicly so automated integration tests can drive it deterministically.
    /// </summary>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.Attempts < _options.MaxAttempts)
            .OrderBy(m => m.OccurredOn)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return 0;
        }

        var processedCount = 0;
        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = OutboxRegistry.Deserialize(message.ContractName, message.Payload);
                if (integrationEvent is not null)
                {
                    await publisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                }

                message.ProcessedOn = _timeProvider.GetUtcNow();
                message.Error = null;
                processedCount++;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                LogProcessingError(_logger, message.Id, message.Attempts, ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return processedCount;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogProcessorStarted(_logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogIterationError(_logger, ex);
            }

            try
            {
                await Task.Delay(_options.IntervalMilliseconds, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        LogProcessorStopped(_logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox processor background service started.")]
    private static partial void LogProcessorStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox processor background service stopped.")]
    private static partial void LogProcessorStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to dispatch outbox message '{MessageId}' (Attempt {Attempt}): {ErrorMessage}")]
    private static partial void LogProcessingError(ILogger logger, Guid messageId, int attempt, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "An unexpected error occurred during outbox processor iteration.")]
    private static partial void LogIterationError(ILogger logger, Exception exception);
}
