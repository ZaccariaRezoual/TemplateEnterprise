namespace EnterpriseFramework.Modules.Auth.Contracts;

/// <summary>
/// Body returned by register, login and refresh.
///
/// Deliberately does NOT contain the refresh token: that travels only in an
/// httpOnly cookie, out of reach of any script (XSS containment). The access
/// token is meant to be held in memory by the client, never persisted.
/// </summary>
/// <param name="AccessToken">Signed JWT to send as a Bearer header.</param>
/// <param name="AccessTokenExpiresAtUtc">Expiry of the access token.</param>
/// <param name="User">The authenticated account.</param>
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, UserDto User);

/// <summary>
/// Full session produced by the handlers, INCLUDING the raw refresh token.
/// Consumed only by the endpoint layer, which moves the refresh token into the
/// cookie and forwards the rest as <see cref="AuthResponse"/>. Never serialize
/// this type.
/// </summary>
/// <param name="Response">Client-visible part of the session.</param>
/// <param name="RawRefreshToken">Raw refresh token for the httpOnly cookie.</param>
public sealed record AuthSession(AuthResponse Response, string RawRefreshToken);
