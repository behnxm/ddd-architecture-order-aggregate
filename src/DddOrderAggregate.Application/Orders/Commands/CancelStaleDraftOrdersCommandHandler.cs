using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Specifications;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record CancelStaleDraftOrdersCommand(int OlderThanHours);

/// <summary>
/// Maintenance use case: identifies and cancels stale unsubmitted draft orders using the shared specification.
/// </summary>
public sealed class CancelStaleDraftOrdersCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelStaleDraftOrdersCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CancelStaleDraftOrdersResponse>> HandleAsync(
        CancelStaleDraftOrdersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.OlderThanHours <= 0)
        {
            return Result.Failure<CancelStaleDraftOrdersResponse>(
                Error.Validation("Maintenance.InvalidThreshold", "olderThanHours must be a positive integer."));
        }

        var now = _timeProvider.GetUtcNow();
        var cutoff = now.AddHours(-command.OlderThanHours);
        var spec = new StaleDraftOrdersSpecification(cutoff);

        var staleOrders = await _repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var reason = $"Cancelled by maintenance: draft inactive for more than {command.OlderThanHours} hours.";

        foreach (var order in staleOrders)
        {
            order.Cancel(reason, now);
        }

        if (staleOrders.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new CancelStaleDraftOrdersResponse(staleOrders.Count);
    }
}
