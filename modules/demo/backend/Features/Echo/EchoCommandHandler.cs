using EnterpriseFramework.Application.Abstractions;
using MediatR;

namespace EnterpriseFramework.Modules.Demo.Features.Echo;

/// <summary>
/// Handles <see cref="EchoCommand"/>.
///
/// Side effects: publishes <see cref="DemoEchoedEvent"/> on the event bus so
/// any module can react without this handler knowing the subscribers
/// (event-driven rule). Input is already validated by the pipeline.
/// </summary>
public sealed class EchoCommandHandler : IRequestHandler<EchoCommand, EchoResponse>
{
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="eventBus">Bus used to publish the echo event.</param>
    public EchoCommandHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// Publishes the event and returns the echoed text.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The echo response.</returns>
    public async Task<EchoResponse> Handle(
        EchoCommand request,
        CancellationToken cancellationToken
    )
    {
        await _eventBus
            .PublishAsync(new DemoEchoedEvent(request.Text), cancellationToken)
            .ConfigureAwait(false);

        return new EchoResponse(request.Text, request.Text.Length);
    }
}
