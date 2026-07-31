using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Api.Security;

/// <summary>
/// Builds authorization policies for permission requirements on demand.
///
/// Without it, every permission would have to be registered as a named policy
/// at startup — meaning the host would need to know the permission catalogue
/// of every installed module. Instead, a policy named "permission:users.read"
/// is materialized the first time an endpoint asks for it, so modules declare
/// permissions freely and the host stays agnostic.
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="options">Authorization options (default and fallback policies).</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options) { }

    /// <summary>
    /// Returns the policy for a name, generating permission policies on the fly.
    /// </summary>
    /// <param name="policyName">Requested policy name.</param>
    /// <returns>
    /// A policy requiring authentication plus the matching permission claim,
    /// or the base provider's result for any other policy name.
    /// </returns>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionEndpointExtensions.PolicyPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName).ConfigureAwait(false);
        }

        var permission = policyName[PermissionEndpointExtensions.PolicyPrefix.Length..];
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(PermissionClaims.Permission, permission)
            .Build();
    }
}
