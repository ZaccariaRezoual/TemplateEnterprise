using System.Security.Claims;
using EnterpriseFramework.Application.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EnterpriseFramework.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the current request's claims principal.
/// Registered as scoped: each request gets the identity the JWT bearer
/// middleware validated for it.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Initializes the accessor-backed implementation.
    /// </summary>
    /// <param name="accessor">Provides the current HttpContext.</param>
    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var sub =
                Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll(PermissionClaims.Permission).Select(c => c.Value).ToArray() ?? [];
}
