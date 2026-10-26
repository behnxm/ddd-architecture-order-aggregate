using System.Reflection;
using DddOrderAggregate.Api.Endpoints;
using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Infrastructure.Persistence;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IOrderReadRepository).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    private static readonly Assembly ApiAssembly = typeof(OrderEndpoints).Assembly;

    [Fact]
    public void Domain_DoesNotDependOn_AnyOtherLayerOrFrameworkLibraries()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "DddOrderAggregate.Application",
                "DddOrderAggregate.Infrastructure",
                "DddOrderAggregate.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_DoesNotDependOn_InfrastructureOrApiOrEntityFramework()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "DddOrderAggregate.Infrastructure",
                "DddOrderAggregate.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_DoesNotDependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("DddOrderAggregate.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_DoNotUse_InfrastructureOrDbContextTypes()
    {
        var endpointMethods = typeof(OrderEndpoints).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var method in endpointMethods)
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType);
            parameterTypes.ShouldNotContain(t => t.Namespace != null && t.Namespace.StartsWith("DddOrderAggregate.Infrastructure", StringComparison.Ordinal));
            parameterTypes.ShouldNotContain(t => t.Namespace != null && t.Namespace.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EntitiesAndAggregateRoots_HaveNoPublicSetters()
    {
        var entityTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        (t.BaseType is { IsGenericType: true } &&
                         (t.BaseType.GetGenericTypeDefinition() == typeof(Entity<>) ||
                          t.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))));

        foreach (var type in entityTypes)
        {
            var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in publicProperties)
            {
                var setter = property.SetMethod;
                (setter == null || !setter.IsPublic).ShouldBeTrue($"Property '{property.Name}' on '{type.Name}' has a public setter.");
            }
        }
    }

    [Fact]
    public void OrderLine_Mutators_AreNotPublic()
    {
        var mutators = typeof(OrderLine)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && (m.Name.StartsWith("Change", StringComparison.Ordinal) || m.Name.StartsWith("Merge", StringComparison.Ordinal)));

        mutators.ShouldBeEmpty();
    }

    [Fact]
    public void StronglyTypedIds_AreReadOnlyRecordStructs()
    {
        var stronglyTypedIdInterface = typeof(IStronglyTypedId<>);
        var idTypes = DomainAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == stronglyTypedIdInterface));

        foreach (var type in idTypes)
        {
            type.IsValueType.ShouldBeTrue($"Type '{type.Name}' implementing IStronglyTypedId must be a struct.");
        }
    }

    [Fact]
    public void ValueObjects_HaveNoPublicConstructorsOrSetters()
    {
        var valueObjectTypes = new[] { typeof(Money), typeof(Address), typeof(Quantity), typeof(Currency) };

        foreach (var type in valueObjectTypes)
        {
            if (type.IsClass)
            {
                var publicConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                publicConstructors.ShouldBeEmpty($"Value object '{type.Name}' must not expose public constructors.");

                var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in publicProperties)
                {
                    (prop.SetMethod == null || !prop.SetMethod.IsPublic).ShouldBeTrue($"Value object '{type.Name}' property '{prop.Name}' has a public setter.");
                }
            }
        }
    }

    [Fact]
    public void DomainEvents_AreSealedRecords_EndingInDomainEvent()
    {
        var domainEventInterface = typeof(IDomainEvent);
        var eventTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && domainEventInterface.IsAssignableFrom(t));

        foreach (var type in eventTypes)
        {
            type.IsSealed.ShouldBeTrue($"Domain event '{type.Name}' must be sealed.");
            type.Name.EndsWith("DomainEvent", StringComparison.Ordinal).ShouldBeTrue($"Domain event '{type.Name}' must end in 'DomainEvent'.");
        }
    }

    [Fact]
    public void IntegrationEvents_LiveOnlyInApplication_AndEndInIntegrationEvent()
    {
        var integrationEventInterface = typeof(IIntegrationEvent);
        var eventTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && integrationEventInterface.IsAssignableFrom(t));

        eventTypes.ShouldNotBeEmpty();

        foreach (var type in eventTypes)
        {
            type.IsSealed.ShouldBeTrue($"Integration event '{type.Name}' must be sealed.");
            type.Name.EndsWith("IntegrationEvent", StringComparison.Ordinal).ShouldBeTrue($"Integration event '{type.Name}' must end in 'IntegrationEvent'.");
        }

        // Domain must never reference IIntegrationEvent
        DomainAssembly.GetTypes()
            .Any(t => integrationEventInterface.IsAssignableFrom(t))
            .ShouldBeFalse("Domain assembly must not declare or implement IIntegrationEvent.");
    }

    [Fact]
    public void RepositoryInterfaces_ExistOnlyForAggregateRoots()
    {
        var repositoryInterfaces = DomainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        repositoryInterfaces.Select(i => i.Name).ShouldBe(["IOrderRepository"]);
    }

    [Fact]
    public void UseCaseHandlers_AreSealed_AndEndInHandler()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        (t.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                         t.Name.EndsWith("QueryHandler", StringComparison.Ordinal)));

        handlerTypes.ShouldNotBeEmpty();

        foreach (var handler in handlerTypes)
        {
            handler.IsSealed.ShouldBeTrue($"Handler '{handler.Name}' must be sealed.");
        }
    }
}
