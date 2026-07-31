using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Users.Domain;

/// <summary>
/// This module's view of an account.
///
/// It is a PROJECTION, not the source of truth: the account itself lives in
/// the Auth module, and this row is created and updated by subscribing to
/// Auth's public events. Duplicating email and display name is deliberate —
/// it lets Users query, sort and page without reaching into another module's
/// schema, which is what keeps modules separately deployable and removable.
/// </summary>
public sealed class UserProfile : EntityBase<Guid>
{
    private UserProfile() { }

    /// <summary>Email address, mirrored from the Auth account.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Name shown in the UI; editable here.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Optional job title, owned by this module.</summary>
    public string? JobTitle { get; private set; }

    /// <summary>
    /// Whether the account is active. Deactivating hides the user from lists
    /// without deleting history; account deletion belongs to Auth.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>UTC instant the projection row was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates the projection for a newly registered account.
    /// </summary>
    /// <param name="userId">Account identifier from Auth (the projection's key).</param>
    /// <param name="email">Email of the account.</param>
    /// <param name="displayName">Name shown in the UI.</param>
    /// <returns>The new profile.</returns>
    public static UserProfile FromRegistration(Guid userId, string email, string displayName) =>
        new()
        {
            Id = userId,
            Email = email,
            DisplayName = displayName,
            CreatedAtUtc = DateTime.UtcNow,
        };

    /// <summary>
    /// Updates the editable profile fields.
    /// </summary>
    /// <param name="displayName">New display name.</param>
    /// <param name="jobTitle">New job title, or null to clear it.</param>
    public void Update(string displayName, string? jobTitle)
    {
        DisplayName = displayName;
        JobTitle = jobTitle;
    }

    /// <summary>
    /// Activates or deactivates the profile.
    /// </summary>
    /// <param name="isActive">Whether the account should appear as active.</param>
    public void SetActive(bool isActive) => IsActive = isActive;
}
