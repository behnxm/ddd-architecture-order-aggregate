using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DddOrderAggregate.Api.Requests;
using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Infrastructure.Catalog;
using DddOrderAggregate.IntegrationTests.Common;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.IntegrationTests;

public class ApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.EnsureDatabaseCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FullOrderLifecycle_SucceedsOverHttp()
    {
        var customerId = Guid.NewGuid();

        // 1. POST /api/v1/orders -> 201 Created
        var createRequest = new CreateOrderRequest(customerId, "USD");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        createResponse.Headers.ETag.ShouldNotBeNull();
        createResponse.Headers.ETag.Tag.ShouldBe("\"1\"");

        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        order.ShouldNotBeNull();
        order.Status.ShouldBe("Draft");
        order.Total.ShouldBe(0m);
        order.Version.ShouldBe(1);

        var orderId = order.Id;

        // 2. POST /api/v1/orders/{id}/lines -> 200 OK
        var addLineRequest = new AddOrderLineRequest(InMemoryProductCatalog.KeyboardId, 2);
        _client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var addLineResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/lines", addLineRequest);
        _client.DefaultRequestHeaders.Remove("If-Match");

        addLineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        addLineResponse.Headers.ETag!.Tag.ShouldBe("\"2\"");

        var withLine = await addLineResponse.Content.ReadFromJsonAsync<OrderResponse>();
        withLine!.Lines.Count.ShouldBe(1);
        withLine.Total.ShouldBe(299.98m);
        withLine.Version.ShouldBe(2);

        // 3. PUT /api/v1/orders/{id}/shipping-address -> 200 OK
        var addressRequest = new SetShippingAddressRequest("123 Tech Blvd", "San Francisco", "94105", "US");
        _client.DefaultRequestHeaders.Add("If-Match", "\"2\"");
        var setAddressResponse = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/shipping-address", addressRequest);
        _client.DefaultRequestHeaders.Remove("If-Match");

        setAddressResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        setAddressResponse.Headers.ETag!.Tag.ShouldBe("\"3\"");

        // 4. POST /api/v1/orders/{id}/submit -> 200 OK
        _client.DefaultRequestHeaders.Add("If-Match", "\"3\"");
        var submitResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/submit", null);
        _client.DefaultRequestHeaders.Remove("If-Match");

        submitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        submitResponse.Headers.ETag!.Tag.ShouldBe("\"4\"");

        var submittedOrder = await submitResponse.Content.ReadFromJsonAsync<OrderResponse>();
        submittedOrder!.Status.ShouldBe("Submitted");
        submittedOrder.SubmittedAt.ShouldNotBeNull();

        // 5. POST /api/v1/orders/{id}/pay -> 200 OK
        _client.DefaultRequestHeaders.Add("If-Match", "\"4\"");
        var payResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/pay", null);
        _client.DefaultRequestHeaders.Remove("If-Match");

        payResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        payResponse.Headers.ETag!.Tag.ShouldBe("\"5\"");

        var paidOrder = await payResponse.Content.ReadFromJsonAsync<OrderResponse>();
        paidOrder!.Status.ShouldBe("Paid");

        // 6. POST /api/v1/orders/{id}/ship -> 200 OK
        _client.DefaultRequestHeaders.Add("If-Match", "\"5\"");
        var shipResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/ship", null);
        _client.DefaultRequestHeaders.Remove("If-Match");

        shipResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        shipResponse.Headers.ETag!.Tag.ShouldBe("\"6\"");

        var shippedOrder = await shipResponse.Content.ReadFromJsonAsync<OrderResponse>();
        shippedOrder!.Status.ShouldBe("Shipped");
        shippedOrder.ShippedAt.ShouldNotBeNull();

        // 7. GET /api/v1/orders/{id} -> 200 OK with ETag
        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Headers.ETag!.Tag.ShouldBe("\"6\"");
    }

    [Fact]
    public async Task IfMatch_Mismatch_Returns412PreconditionFailed()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(Guid.NewGuid(), "USD"));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Sending expected version 99 while order is version 1
        _client.DefaultRequestHeaders.Add("If-Match", "\"99\"");
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/orders/{order!.Id}/lines",
            new AddOrderLineRequest(InMemoryProductCatalog.KeyboardId, 1));
        _client.DefaultRequestHeaders.Remove("If-Match");

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(412);
        problem.Extensions.ContainsKey("code").ShouldBeTrue();
        problem.Extensions["code"]!.ToString().ShouldBe("Order.VersionMismatch");
    }

    [Fact]
    public async Task AddLine_AfterSubmit_Returns409Conflict()
    {
        // Create, add line, set address, and submit
        var createRes = await _client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(Guid.NewGuid(), "USD"));
        var order = await createRes.Content.ReadFromJsonAsync<OrderResponse>();

        await _client.PostAsJsonAsync($"/api/v1/orders/{order!.Id}/lines", new AddOrderLineRequest(InMemoryProductCatalog.KeyboardId, 1));
        await _client.PutAsJsonAsync($"/api/v1/orders/{order.Id}/shipping-address", new SetShippingAddressRequest("123 St", "City", "12345", "US"));
        await _client.PostAsync($"/api/v1/orders/{order.Id}/submit", null);

        // Attempting to add a line to Submitted order must return 409
        var addRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/lines", new AddOrderLineRequest(InMemoryProductCatalog.MouseId, 1));
        addRes.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await addRes.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(409);
        problem.Extensions["code"]!.ToString().ShouldBe("Order.NotDraft");
    }

    [Fact]
    public async Task InvalidRequestBody_Returns400ProblemDetails()
    {
        // Missing required Currency and empty Guid
        var invalidPayload = new { CustomerId = Guid.Empty, Currency = "" };
        var response = await _client.PostAsJsonAsync("/api/v1/orders", invalidPayload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(400);
    }

    [Fact]
    public async Task UnknownOrder_Returns404NotFound()
    {
        var randomId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/orders/{randomId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(404);
        problem.Extensions["code"]!.ToString().ShouldBe("Order.NotFound");
    }

    [Fact]
    public async Task HealthEndpoints_ReturnHealthyStatus()
    {
        var livenessRes = await _client.GetAsync("/health");
        livenessRes.StatusCode.ShouldBe(HttpStatusCode.OK);

        var readinessRes = await _client.GetAsync("/health/ready");
        readinessRes.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_HasUniqueOperationIdsAcrossAllEndpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonNode.Parse(json);
        doc.ShouldNotBeNull();

        var paths = doc["paths"]!.AsObject();
        var operationIds = new List<string>();

        foreach (var path in paths)
        {
            var methods = path.Value!.AsObject();
            foreach (var method in methods)
            {
                var opId = method.Value?["operationId"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(opId))
                {
                    operationIds.Add(opId);
                }
            }
        }

        operationIds.Count.ShouldBeGreaterThanOrEqualTo(11);
        operationIds.Distinct().Count().ShouldBe(operationIds.Count);
    }

    [Fact]
    public async Task Scalar_ServesDocumentationHtml()
    {
        var response = await _client.GetAsync("/scalar/v1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("scalar");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/scalar")]
    public async Task RootAndScalarEndpoints_RedirectToScalarV1(string path)
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldBe("/scalar/v1");
    }
}
