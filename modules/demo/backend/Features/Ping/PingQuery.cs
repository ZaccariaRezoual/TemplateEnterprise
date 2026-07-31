using MediatR;

namespace EnterpriseFramework.Modules.Demo.Features.Ping;

/// <summary>
/// Sample query proving the MediatR pipeline end-to-end.
/// Returns a static message with the server UTC time.
/// </summary>
public sealed record PingQuery : IRequest<PingResponse>;

/// <summary>
/// Response contract of <see cref="PingQuery"/> (exposed by the API and,
/// through OpenAPI, by the generated SDK).
/// </summary>
/// <param name="Message">Static confirmation message.</param>
/// <param name="TimestampUtc">Server UTC time at which the query was handled.</param>
public sealed record PingResponse(string Message, DateTime TimestampUtc);

/// <summary>
/// Handles <see cref="PingQuery"/>. No business logic: exists to prove the
/// query side of the pipeline (routing → MediatR → response).
/// </summary>
public sealed class PingQueryHandler : IRequestHandler<PingQuery, PingResponse>
{
    /// <summary>
    /// Builds the pong response.
    /// </summary>
    /// <param name="request">The incoming query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The pong response with the current UTC time.</returns>
    public Task<PingResponse> Handle(PingQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new PingResponse("pong", DateTime.UtcNow));
}
