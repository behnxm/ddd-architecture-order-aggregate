using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record RemoveOrderLineCommand(
    Guid OrderId,
    Guid LineId,
    int? ExpectedVersion = null);

/// <summary>
/// Orchestrates removing an existing line item from an order.
/// </summary>
public sealed class RemoveOrderLineCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RemoveOrderLineCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(RemoveOrderLineCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var orderId = OrderId.From(command.OrderId);
        var lineId = OrderLineId.From(command.LineId);

        return await OrderExecutionHelper.ExecuteMutationAsync(
            _repository,
            _unitOfWork,
            orderId,
            command.ExpectedVersion,
            order => order.RemoveLine(lineId, _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
    }
}
