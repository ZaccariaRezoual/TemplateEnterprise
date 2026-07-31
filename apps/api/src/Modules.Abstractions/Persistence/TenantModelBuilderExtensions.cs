using System.Linq.Expressions;
using System.Reflection;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Abstractions.Persistence;

/// <summary>
/// Applies tenant isolation to a module's EF model.
///
/// A module opts in with ONE call in <c>OnModelCreating</c>; every entity
/// implementing <see cref="ITenantOwned"/> then gets a global query filter.
/// The point is that isolation stops being something a developer has to
/// remember on every query — the failure mode of manual filtering is silent,
/// and one forgotten clause leaks another customer's data with no error.
/// </summary>
public static class TenantModelBuilderExtensions
{
    private static readonly MethodInfo ApplyToEntityMethod =
        typeof(TenantModelBuilderExtensions).GetMethod(
            nameof(ApplyToEntity),
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

    /// <summary>
    /// Adds a tenant query filter to every <see cref="ITenantOwned"/> entity
    /// in the model.
    /// </summary>
    /// <param name="modelBuilder">The model being configured.</param>
    /// <param name="tenantContext">
    /// Tenant of the current request. Pass the context's own injected
    /// instance: EF re-evaluates it per query, so the filter follows the
    /// request rather than being baked in at startup.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    ///
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ApplyTenantFilters(_tenantContext);
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplyTenantFilters(
        this ModelBuilder modelBuilder,
        ITenantContext tenantContext
    )
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ApplyToEntityMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder, tenantContext]);
        }

        return modelBuilder;
    }

    private static void ApplyToEntity<TEntity>(
        ModelBuilder modelBuilder,
        ITenantContext tenantContext
    )
        where TEntity : class, ITenantOwned
    {
        // Two clauses, both necessary:
        // - IsMultiTenant makes the filter inert in single-tenant deployments,
        //   so the same code runs with no tenancy behaviour at all.
        // - Comparing against TenantId (a captured instance member) is what
        //   makes EF treat it as a query parameter re-read per request.
        Expression<Func<TEntity, bool>> filter = entity =>
            !tenantContext.IsMultiTenant || entity.TenantId == tenantContext.TenantId;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
