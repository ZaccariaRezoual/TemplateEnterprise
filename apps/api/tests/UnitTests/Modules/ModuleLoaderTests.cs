using EnterpriseFramework.Modules.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Modules;

public sealed class ModuleLoaderTests
{
    [Fact]
    public void Resolve_OrdersModulesByDependency()
    {
        var modules = new[]
        {
            Module("Users", dependencies: ["Auth"]),
            Module("Auth"),
            Module("Notifications", dependencies: ["Users"]),
        };

        var resolved = ModuleLoader.Resolve(modules, EmptyConfiguration());

        var names = resolved.Select(m => m.Manifest.Name).ToArray();
        Array.IndexOf(names, "Auth").ShouldBeLessThan(Array.IndexOf(names, "Users"));
        Array.IndexOf(names, "Users").ShouldBeLessThan(Array.IndexOf(names, "Notifications"));
    }

    [Fact]
    public void Resolve_SkipsModulesDisabledInManifest()
    {
        var modules = new[] { Module("Auth"), Module("Chat", enabled: false) };

        var resolved = ModuleLoader.Resolve(modules, EmptyConfiguration());

        resolved.Select(m => m.Manifest.Name).ShouldBe(["Auth"]);
    }

    [Fact]
    public void Resolve_ConfigurationOverrideDisablesModule()
    {
        var modules = new[] { Module("Auth"), Module("Chat") };
        var configuration = Configuration(("Modules:Chat:Enabled", "false"));

        var resolved = ModuleLoader.Resolve(modules, configuration);

        resolved.Select(m => m.Manifest.Name).ShouldBe(["Auth"]);
    }

    [Fact]
    public void Resolve_ThrowsWhenDependencyIsDisabled()
    {
        var modules = new[] { Module("Auth", enabled: false), Module("Users", dependencies: ["Auth"]) };

        var act = () => ModuleLoader.Resolve(modules, EmptyConfiguration());

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("Auth");
    }

    [Fact]
    public void Resolve_ThrowsOnDependencyCycle()
    {
        var modules = new[]
        {
            Module("A", dependencies: ["B"]),
            Module("B", dependencies: ["A"]),
        };

        var act = () => ModuleLoader.Resolve(modules, EmptyConfiguration());

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("cycle");
    }

    private static LoadedModule Module(
        string name,
        string[]? dependencies = null,
        bool enabled = true
    ) =>
        new(
            new StubModule(name),
            new ModuleManifest
            {
                Name = name,
                Version = "1.0.0",
                Dependencies = dependencies ?? [],
                Enabled = enabled,
            }
        );

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();

    private sealed class StubModule(string name) : IModule
    {
        public string Name => name;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

        public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
    }
}
