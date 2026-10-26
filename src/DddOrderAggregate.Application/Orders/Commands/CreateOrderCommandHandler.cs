using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record CreateOrderCommand(Guid CustomerId, string Currency);

/// <summary>
/// Orchestrates creation of a new Draft order aggregate root.
/// </summary>
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currencyResult = Currency.FromCode(command.Currency);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        var customerId = CustomerId.From(command.CustomerId);
        var orderResult = Order.CreateDraft(customerId, currencyResult.Value, _timeProvider.GetUtcNow());
        if (orderResult.IsFailure)
        {
            return orderResult.Error;
        }

        var order = orderResult.Value;
        await _repository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return order.ToResponse();
    }
}
