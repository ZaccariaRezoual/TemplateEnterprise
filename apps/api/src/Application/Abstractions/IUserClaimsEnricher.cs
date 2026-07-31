using System.Security.Claims;

namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Extension point for adding claims to an access token being issued.
///
/// It exists so the Auth module can stay unaware of authorization: Auth owns
/// AUTHENTICATION (who you are) and calls every registered enricher while
/// building a token; the Authorization module owns ROLES AND PERMISSIONS
/// (what you may do) and registers an enricher that adds them.
///
/// With the Authorization module disabled no enricher is registered, tokens
/// simply carry no role/permission claims, and nothing breaks — which is what
/// makes both modules independently removable.
/// </summary>
public interface IUserClaimsEnricher
{
    /// <summary>
    /// Produces the additional claims for an account.
    ///
    /// Called once per issued token (login, register and every refresh), so a
    /// permission change takes effect at the next refresh — at most one
    /// access-token lifetime later.
    /// </summary>
    /// <param name="userId">Account the token is being issued for.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Claims to embed; empty when the enricher has nothing to add.</returns>
    Task<IReadOnlyCollection<Claim>> GetClaimsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
