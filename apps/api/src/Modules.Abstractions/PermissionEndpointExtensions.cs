using Microsoft.AspNetCore.Builder;

namespace EnterpriseFramework.Modules.Abstractions;

/// <summary>
/// Declares the permission an endpoint requires.
///
/// Lives here, not in the Authorization module, so ANY module can protect its
/// endpoints without referencing the module that happens to grant permissions
/// today. The host turns the requirement into a policy; if the Authorization
/// module is disabled, no token carries permission claims and every protected
/// endpoint denies — failing closed, which is the safe direction.
/// </summary>
public static class PermissionEndpointExtensions
{
    /// <summary>Prefix of the dynamically generated permission policies.</summary>
    public const string PolicyPrefix = "permission:";

    /// <summary>
    /// Requires the caller to hold a permission.
    /// </summary>
    /// <typeparam name="TBuilder">Endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or group being configured.</param>
    /// <param name="permission">Permission name, e.g. "users.read".</param>
    /// <returns>The builder, for chaining.</returns>
    ///
    /// <example>
    /// <code>
    /// group.MapGet("/", Handler).RequirePermission(Permissions.Users.Read);
    /// </code>
    /// </example>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        (TBuilder)builder.RequireAuthorization($"{PolicyPrefix}{permission}");
}
