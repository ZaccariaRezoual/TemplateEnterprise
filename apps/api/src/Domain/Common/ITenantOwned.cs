namespace EnterpriseFramework.Domain.Common;

/// <summary>
/// Marks an entity as belonging to one tenant.
///
/// Implementing it is how a module opts INTO multi-tenancy: the shared EF
/// configuration then applies a global query filter, so every query is scoped
/// to the current tenant without a single `Where` in application code. That
/// matters because the failure mode of manual filtering is silent — one
/// forgotten clause leaks another customer's data with no error anywhere.
///
/// It lives in Domain because "this row belongs to a tenant" is a property of
/// the model, not of the infrastructure that enforces it.
/// </summary>
public interface ITenantOwned
{
    /// <summary>
    /// Tenant this row belongs to. Assigned on creation and never changed:
    /// moving a row between tenants is a data migration, not an update.
    /// </summary>
    Guid TenantId { get; }
}
