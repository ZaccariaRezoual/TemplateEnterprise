using System.Globalization;

namespace EnterpriseFramework.Modules.Realtime.Hubs;

/// <summary>
/// Builds the SignalR group names the server assigns to connections.
///
/// Centralized so the naming exists once: a mismatch between the code that
/// JOINS a group and the code that SENDS to it produces no error, just
/// silence — the hardest kind of bug to notice in a realtime system.
///
/// Clients never choose groups. Every name here is derived from the
/// authenticated principal at connection time.
/// </summary>
public static class RealtimeGroups
{
    /// <summary>
    /// Group carrying every connection of one account (a user may have
    /// several tabs or devices open).
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <returns>The group name.</returns>
    public static string ForUser(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"user:{userId:N}");

    /// <summary>
    /// Group carrying every connection whose account holds a role.
    ///
    /// Scoped by tenant when multi-tenancy is on: without it, an event sent
    /// to "role:Administrator" would reach the administrators of EVERY
    /// customer — the one broadcast that silently crosses the isolation
    /// boundary the query filters protect everywhere else.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="tenantId">Tenant, or null in single-tenant deployments.</param>
    /// <returns>The group name.</returns>
    public static string ForRole(string role, Guid? tenantId = null) =>
        tenantId is { } tenant
            ? string.Create(CultureInfo.InvariantCulture, $"tenant:{tenant:N}:role:{role}")
            : $"role:{role}";

    /// <summary>
    /// Group carrying every connection of one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>The group name.</returns>
    public static string ForTenant(Guid tenantId) =>
        string.Create(CultureInfo.InvariantCulture, $"tenant:{tenantId:N}");
}
