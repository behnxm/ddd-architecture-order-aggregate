using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record AddOrderLineCommand(
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    int? ExpectedVersion = null);

/// <summary>
/// Orchestrates adding a catalog product snapshot as an order line item.
/// </summary>
public sealed class AddOrderLineCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCatalog _productCatalog;
    private readonly TimeProvider _timeProvider;

    public AddOrderLineCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        IProductCatalog productCatalog,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _productCatalog = productCatalog;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(AddOrderLineCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var quantityResult = Quantity.Create(command.Quantity);
        if (quantityResult.IsFailure)
        {
            return quantityResult.Error;
        }

        var productId = ProductId.From(command.ProductId);
        var productResult = await _productCatalog.GetProductAsync(productId, cancellationToken).ConfigureAwait(false);
        if (productResult.IsFailure)
        {
            return productResult.Error;
        }

        var product = productResult.Value;
        var orderId = OrderId.From(command.OrderId);

        return await OrderExecutionHelper.ExecuteMutationAsync(
            _repository,
            _unitOfWork,
            orderId,
            command.ExpectedVersion,
            order => order.AddLine(product.ProductId, product.Name, product.UnitPrice, quantityResult.Value, _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
    }
}
