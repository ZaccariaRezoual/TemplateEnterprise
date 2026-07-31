using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EnterpriseFramework.Modules.Abstractions;

/// <summary>
/// Discovers, filters and orders backend modules at startup.
///
/// Pipeline: scan the given assemblies for <see cref="IModule"/> implementations
/// â†’ read each assembly's embedded <c>module.json</c> manifest â†’ apply the
/// "Modules:{Name}:Enabled" configuration override â†’ validate dependencies â†’
/// return modules topologically sorted so dependencies initialize first.
///
/// Fail-fast philosophy: a missing manifest, a dependency on a disabled/unknown
/// module or a dependency cycle throws at startup instead of degrading at runtime.
/// </summary>
public static class ModuleLoader
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Loads the enabled modules from the given assemblies, ordered by dependencies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan (one module per assembly).</param>
    /// <param name="configuration">Host configuration, used for enable/disable overrides.</param>
    /// <returns>Enabled modules in initialization order.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a manifest is missing/invalid, a dependency is not satisfied
    /// or the dependency graph contains a cycle.
    /// </exception>
    public static IReadOnlyList<LoadedModule> Load(
        IEnumerable<Assembly> assemblies,
        IConfiguration configuration
    )
    {
        var loaded = assemblies
            .SelectMany(DiscoverModules)
            .Select(module => new LoadedModule(
                module,
                ReadManifest(module.GetType().Assembly, module.Name)
            ))
            .ToList();

        return Resolve(loaded, configuration);
    }

    /// <summary>
    /// Filters disabled modules and topologically sorts the rest by dependency.
    /// Pure logic, exposed separately from assembly scanning to be unit-testable.
    /// </summary>
    /// <param name="modules">Discovered modules with their manifests.</param>
    /// <param name="configuration">Host configuration, used for enable/disable overrides.</param>
    /// <returns>Enabled modules in initialization order.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an enabled module depends on a disabled/unknown module or
    /// the graph contains a cycle.
    /// </exception>
    public static IReadOnlyList<LoadedModule> Resolve(
        IReadOnlyCollection<LoadedModule> modules,
        IConfiguration configuration
    )
    {
        var enabled = modules
            .Where(m =>
                configuration.GetValue<bool?>($"Modules:{m.Manifest.Name}:Enabled")
                ?? m.Manifest.Enabled
            )
            .ToDictionary(m => m.Manifest.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var module in enabled.Values)
        {
            foreach (var dependency in module.Manifest.Dependencies)
            {
                if (!enabled.ContainsKey(dependency))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Manifest.Name}' depends on '{dependency}', "
                            + "which is missing or disabled."
                    );
                }
            }
        }

        // Depth-first topological sort with cycle detection.
        var ordered = new List<LoadedModule>();
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in enabled.Keys)
        {
            Visit(name);
        }

        return ordered;

        void Visit(string name)
        {
            if (visited.Contains(name))
            {
                return;
            }

            if (!visiting.Add(name))
            {
                throw new InvalidOperationException(
                    $"Dependency cycle detected involving module '{name}'."
                );
            }

            foreach (var dependency in enabled[name].Manifest.Dependencies)
            {
                Visit(dependency);
            }

            visiting.Remove(name);
            visited.Add(name);
            ordered.Add(enabled[name]);
        }
    }

    private static IEnumerable<IModule> DiscoverModules(Assembly assembly) =>
        assembly
            .GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t =>
                (IModule)(
                    Activator.CreateInstance(t)
                    ?? throw new InvalidOperationException(
                        $"Could not instantiate module type '{t.FullName}'."
                    )
                )
            );

    private static ModuleManifest ReadManifest(Assembly assembly, string moduleName)
    {
        using var stream =
            assembly.GetManifestResourceStream("module.json")
            ?? throw new InvalidOperationException(
                $"Module '{moduleName}' ({assembly.GetName().Name}) has no embedded "
                    + "'module.json'. Embed it with LogicalName=\"module.json\"."
            );

        var manifest =
            JsonSerializer.Deserialize<ModuleManifest>(stream, ManifestJsonOptions)
            ?? throw new InvalidOperationException(
                $"Module '{moduleName}': embedded 'module.json' is empty or invalid."
            );

        if (!string.Equals(manifest.Name, moduleName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Module manifest name '{manifest.Name}' does not match IModule.Name '{moduleName}'."
            );
        }

        return manifest;
    }
}

/// <summary>
/// A discovered module paired with its parsed <c>module.json</c> manifest.
/// </summary>
/// <param name="Instance">The module implementation.</param>
/// <param name="Manifest">The manifest embedded in the module assembly.</param>
public sealed record LoadedModule(IModule Instance, ModuleManifest Manifest);
