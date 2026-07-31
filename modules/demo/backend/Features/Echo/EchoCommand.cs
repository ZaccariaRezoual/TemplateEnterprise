using MediatR;

namespace EnterpriseFramework.Modules.Demo.Features.Echo;

/// <summary>
/// Sample command proving validation and event publishing end-to-end.
/// Echoes the given text back and publishes <see cref="DemoEchoedEvent"/>.
/// </summary>
/// <param name="Text">Text to echo. Required, max 500 characters (see validator).</param>
public sealed record EchoCommand(string Text) : IRequest<EchoResponse>;

/// <summary>
/// Response contract of <see cref="EchoCommand"/>.
/// </summary>
/// <param name="Text">The echoed text.</param>
/// <param name="Length">Length of the echoed text, in characters.</param>
public sealed record EchoResponse(string Text, int Length);
