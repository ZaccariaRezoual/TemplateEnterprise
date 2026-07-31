namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Claim type constants shared by the module that ISSUES permission claims
/// (Authorization) and the host pipeline that READS them.
///
/// They live in Application rather than in either module so neither has to
/// reference the other, and a typo in a magic string cannot silently disable
/// an authorization check.
/// </summary>
public static class PermissionClaims
{
    /// <summary>
    /// Claim type carrying a single granted permission (e.g. "users.read").
    /// A token carries one claim of this type per permission.
    /// </summary>
    public const string Permission = "permission";
}
