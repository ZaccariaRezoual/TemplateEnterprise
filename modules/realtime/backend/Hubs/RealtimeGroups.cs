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
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <returns>The group name.</returns>
    public static string ForRole(string role) => $"role:{role}";
}
