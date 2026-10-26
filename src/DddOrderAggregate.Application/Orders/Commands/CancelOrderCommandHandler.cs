using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record CancelOrderCommand(
    Guid OrderId,
    string Reason,
    int? ExpectedVersion = null);

/// <summary>
/// Orchestrates cancelling an order with a recorded business reason.
/// </summary>
public sealed class CancelOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var orderId = OrderId.From(command.OrderId);

        return await OrderExecutionHelper.ExecuteMutationAsync(
            _repository,
            _unitOfWork,
            orderId,
            command.ExpectedVersion,
            order => order.Cancel(command.Reason, _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
    }
}
