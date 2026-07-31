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
}
