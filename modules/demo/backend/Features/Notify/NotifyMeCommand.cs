using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Notifications.Contracts.Events;
using FluentValidation;
using MediatR;

namespace EnterpriseFramework.Modules.Demo.Features.Notify;

/// <summary>
/// Sends the caller a notification.
///
/// Exists so the realtime path is demonstrable and testable end to end: it
/// publishes a notification request exactly as any real feature would, and
/// everything after that — persistence, the realtime push, the badge and the
/// toast — happens without this module knowing any of it exists.
/// </summary>
/// <param name="Title">Headline of the notification.</param>
/// <param name="Body">Explanatory text.</param>
public sealed record NotifyMeCommand(string Title, string Body) : IRequest;

/// <summary>Validation rules for <see cref="NotifyMeCommand"/>.</summary>
public sealed class NotifyMeCommandValidator : AbstractValidator<NotifyMeCommand>
{
    /// <summary>Initializes the rules.</summary>
    public NotifyMeCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(2000);
    }
}

/// <summary>
/// Handles <see cref="NotifyMeCommand"/> by publishing a notification request
/// on the event bus.
/// </summary>
public sealed class NotifyMeCommandHandler : IRequestHandler<NotifyMeCommand>
{
    private readonly IEventBus _eventBus;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="eventBus">Publishes the notification request.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public NotifyMeCommandHandler(IEventBus eventBus, ICurrentUser currentUser)
    {
        _eventBus = eventBus;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Publishes the request for the authenticated caller.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="UnauthorizedException">Thrown when the caller is anonymous.</exception>
    public Task Handle(NotifyMeCommand request, CancellationToken cancellationToken)
    {
        var userId =
            _currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        return _eventBus.PublishAsync(
            new NotificationRequested(
                userId,
                request.Title,
                request.Body,
                NotificationLevel.Success
            ),
            cancellationToken
        );
    }
}
