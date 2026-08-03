namespace EnterpriseFramework.Modules.Storage.Domain;

/// <summary>
/// Who may download a stored file.
///
/// Decided at UPLOAD and explicitly: there is no endpoint that changes it
/// afterwards, because "make this public" is the kind of one-click action
/// that turns a mistake into a disclosure with no review step in between.
/// Re-upload instead — the bytes are cheap, the mistake is not.
///
/// The enum travels as a NAME in JSON (the host registers
/// <c>JsonStringEnumConverter</c>), so inserting a value here never changes
/// the meaning of a value already stored or in flight.
/// </summary>
public enum FileVisibility
{
    /// <summary>
    /// Only an authenticated caller may download it. The DEFAULT, and
    /// deliberately the first value: a default of "public" would turn every
    /// forgotten parameter into a data leak.
    /// </summary>
    Private = 0,

    /// <summary>
    /// Anyone may download it, without a token, through the module's public
    /// endpoint. Chosen for the pictures of a showcase page, which an
    /// anonymous visitor has to be able to load.
    /// </summary>
    Public = 1,
}
