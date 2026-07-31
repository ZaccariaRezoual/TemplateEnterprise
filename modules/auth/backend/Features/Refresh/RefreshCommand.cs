using EnterpriseFramework.Modules.Auth.Contracts;
using MediatR;

namespace EnterpriseFramework.Modules.Auth.Features.Refresh;

/// <summary>
/// Exchanges a valid refresh token for a new session (rotation).
/// The token comes from the httpOnly cookie, read by the endpoint layer —
/// clients never send it in a body.
/// </summary>
/// <param name="RawRefreshToken">Raw refresh token presented by the client.</param>
public sealed record RefreshCommand(string RawRefreshToken) : IRequest<AuthSession>;
