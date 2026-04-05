using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TicketingEngine.ArchitectureTests;

/// <summary>
/// Enforces Clean Architecture dependency rules on every build.
/// These tests prevent accidental coupling between layers as the team grows.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string DomainNs         = "TicketingEngine.Domain";
    private const string ApplicationNs    = "TicketingEngine.Application";
    private const string InfrastructureNs = "TicketingEngine.Infrastructure";
    private const string ApiNs            = "TicketingEngine.API";

    private static readonly System.Reflection.Assembly[] AllAssemblies =
    [
        typeof(TicketingEngine.Domain.Entities.BaseEntity).Assembly,
        typeof(TicketingEngine.Application.Interfaces.IAppDbContext).Assembly,
        typeof(TicketingEngine.Infrastructure.Persistence.AppDbContext).Assembly,
        typeof(TicketingEngine.API.Controllers.EventsController).Assembly
    ];

    [Fact]
    public void Domain_MustNotDependOn_AnyOtherLayer()
    {
        var result = Types.InAssembly(
                typeof(TicketingEngine.Domain.Entities.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain is the innermost layer — zero outward dependencies");
    }

    [Fact]
    public void Application_MustNotDependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(
                typeof(TicketingEngine.Application.Interfaces.IAppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application only depends on Domain and abstractions");
    }

    [Fact]
    public void Infrastructure_MustNotDependOn_Api()
    {
        var result = Types.InAssembly(
                typeof(TicketingEngine.Infrastructure.Persistence.AppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_MustNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(
                typeof(TicketingEngine.API.Controllers.EventsController).Assembly)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Controllers talk to Application, never to Infrastructure");
    }

    [Fact]
    public void CommandHandlers_ShouldBeSealed_AndEndWithHandler()
    {
        var result = Types.InAssembly(
                typeof(TicketingEngine.Application.Interfaces.IAppDbContext).Assembly)
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Handlers are concrete implementations — sealing prevents accidental inheritance");
    }

    [Fact]
    public void DomainEntities_ShouldHavePrivateSetters()
    {
        var entityTypes = typeof(TicketingEngine.Domain.Entities.BaseEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(
                typeof(TicketingEngine.Domain.Entities.BaseEntity)));

        foreach (var type in entityTypes)
        {
            var publicSetters = type.GetProperties()
                .Where(p => p.SetMethod?.IsPublic == true)
                .Select(p => p.Name)
                .ToList();

            publicSetters.Should().BeEmpty(
                because: $"{type.Name} exposes public setters: " +
                         string.Join(", ", publicSetters) +
                         " — use factory methods and domain methods instead");
        }
    }
}
