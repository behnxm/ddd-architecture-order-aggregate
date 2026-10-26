using System.Globalization;
using DddOrderAggregate.Api.Common;
using DddOrderAggregate.Api.Requests;
using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Commands;
using DddOrderAggregate.Application.Orders.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DddOrderAggregate.Api.Endpoints;

/// <summary>
/// Exposes Order aggregate operations following REST and Ubiquitous Language semantics.
/// Endpoints utilize optimistic concurrency via ETag/If-Match headers.
/// Hard deletion is forbidden: orders are cancelled via business state transitions, preserving audit trails.
/// </summary>
public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("Orders");

        // POST /api/v1/orders
        group.MapPost("/", async (
            [FromBody] CreateOrderRequest request,
            [FromServices] CreateOrderCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new CreateOrderCommand(request.CustomerId, request.Currency);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Created($"/api/v1/orders/{result.Value.Id}", result.Value);
        })
        .WithName("CreateDraftOrder")
        .WithSummary("Creates a new draft order")
        .WithDescription("Initializes a new order in Draft status with fixed customer identity and currency.");

        // GET /api/v1/orders/{id}
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] GetOrderQueryHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("GetOrderById")
        .WithSummary("Retrieves an order by ID")
        .WithDescription("Loads the complete aggregate representation and returns an ETag for concurrency control.");

        // GET /api/v1/orders
        group.MapGet("/", async (
            [FromQuery] Guid? customerId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] ListOrdersQueryHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken ct = default) =>
        {
            var result = await handler.HandleAsync(customerId, status, page, pageSize, ct);
            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            return Results.Ok(result.Value);
        })
        .WithName("ListOrders")
        .WithSummary("Lists orders with pagination and filtering")
        .WithDescription("Queries order summaries by customer or lifecycle status, projected directly to read DTOs.");

        // POST /api/v1/orders/{id}/lines
        group.MapPost("/{id:guid}/lines", async (
            [FromRoute] Guid id,
            [FromBody] AddOrderLineRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] AddOrderLineCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new AddOrderLineCommand(id, request.ProductId, request.Quantity, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("AddOrderLine")
        .WithSummary("Adds a product line item to a draft order")
        .WithDescription("Captures product snapshot from the Catalog context. Merges quantity if product already exists on the draft.");

        // PATCH /api/v1/orders/{id}/lines/{lineId}
        group.MapPatch("/{id:guid}/lines/{lineId:guid}", async (
            [FromRoute] Guid id,
            [FromRoute] Guid lineId,
            [FromBody] ChangeOrderLineQuantityRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] ChangeOrderLineQuantityCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new ChangeOrderLineQuantityCommand(id, lineId, request.Quantity, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("ChangeOrderLineQuantity")
        .WithSummary("Updates the quantity of an order line")
        .WithDescription("Modifies line quantity and recalculates the aggregate total while in Draft status.");

        // DELETE /api/v1/orders/{id}/lines/{lineId}
        group.MapDelete("/{id:guid}/lines/{lineId:guid}", async (
            [FromRoute] Guid id,
            [FromRoute] Guid lineId,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] RemoveOrderLineCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new RemoveOrderLineCommand(id, lineId, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("RemoveOrderLine")
        .WithSummary("Removes a line from a draft order")
        .WithDescription("Deletes an item from the draft and recalculates the aggregate total.");

        // PUT /api/v1/orders/{id}/shipping-address
        group.MapPut("/{id:guid}/shipping-address", async (
            [FromRoute] Guid id,
            [FromBody] SetShippingAddressRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] SetShippingAddressCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new SetShippingAddressCommand(
                id,
                request.Street,
                request.City,
                request.PostalCode,
                request.CountryCode,
                expectedVersion);

            var result = await handler.HandleAsync(command, ct);
            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("SetShippingAddress")
        .WithSummary("Sets delivery address on a draft order")
        .WithDescription("Records validated delivery destination value object prior to order submission.");

        // POST /api/v1/orders/{id}/submit
        group.MapPost("/{id:guid}/submit", async (
            [FromRoute] Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] SubmitOrderCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new SubmitOrderCommand(id, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("SubmitOrder")
        .WithSummary("Submits a draft order for fulfillment")
        .WithDescription("Transitions order from Draft to Submitted, locking lines and pricing, and raising OrderSubmittedDomainEvent.");

        // POST /api/v1/orders/{id}/pay
        group.MapPost("/{id:guid}/pay", async (
            [FromRoute] Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] PayOrderCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new PayOrderCommand(id, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("PayOrder")
        .WithSummary("Records payment settlement for an order")
        .WithDescription("Transitions order from Submitted to Paid upon payment gateway confirmation.");

        // POST /api/v1/orders/{id}/ship
        group.MapPost("/{id:guid}/ship", async (
            [FromRoute] Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] ShipOrderCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new ShipOrderCommand(id, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("ShipOrder")
        .WithSummary("Marks a paid order as shipped")
        .WithDescription("Transitions order from Paid to Shipped upon carrier dispatch.");

        // POST /api/v1/orders/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (
            [FromRoute] Guid id,
            [FromBody] CancelOrderRequest request,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromServices] CancelOrderCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var expectedVersion = ParseIfMatch(ifMatch);
            var command = new CancelOrderCommand(id, request.Reason, expectedVersion);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            SetETag(httpContext.Response, result.Value.Version);
            return Results.Ok(result.Value);
        })
        .WithName("CancelOrder")
        .WithSummary("Cancels an order with a reason")
        .WithDescription("Transitions order to Cancelled from Draft, Submitted, or Paid status with a mandatory recorded reason.");

        // POST /api/v1/orders/maintenance/cancel-stale-drafts
        group.MapPost("/maintenance/cancel-stale-drafts", async (
            [FromQuery] int olderThanHours,
            [FromServices] CancelStaleDraftOrdersCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var command = new CancelStaleDraftOrdersCommand(olderThanHours);
            var result = await handler.HandleAsync(command, ct);

            if (result.IsFailure)
            {
                return result.Error.ToHttpResult(httpContext);
            }

            return Results.Ok(result.Value);
        })
        .WithName("CancelStaleDraftOrders")
        .WithSummary("Maintenance: cancels inactive draft orders")
        .WithDescription("Uses the shared StaleDraftOrdersSpecification to purge abandoned drafts older than a threshold.");

        return group;
    }

    private static int? ParseIfMatch(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return null;
        }

        var clean = ifMatch.Trim();
        if (clean.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[2..].Trim();
        }

        clean = clean.Trim('"');
        return int.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version)
            ? version
            : -1;
    }

    private static void SetETag(HttpResponse response, int version)
    {
        response.Headers.ETag = string.Create(CultureInfo.InvariantCulture, $"\"{version}\"");
    }
}
