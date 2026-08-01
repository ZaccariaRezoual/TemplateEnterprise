namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Identity of the caller of the current request.
///
/// Application code and modules depend on this abstraction instead of
/// HttpContext (dependency rule: Application knows nothing about ASP.NET).
/// The Api layer implements it from the authenticated principal's claims.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Whether the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Account id of the caller (the JWT "sub" claim), or null when anonymous.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>Role names of the caller; empty when anonymous.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Permissions granted to the caller, from the token's claims.
    ///
    /// Endpoints normally declare <c>RequirePermission(...)</c> and never read
    /// this. It exists for the cases a policy cannot express — deciding which
    /// of several results to return, or which tiles a dashboard may show —
    /// where the code must branch rather than allow or deny the whole request.
    /// </summary>
    IReadOnlyList<string> Permissions { get; }
}
