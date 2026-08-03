using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features.ArchiveService;

/// <summary>
/// Withdraws a service from the catalogue.
///
/// There is no "delete" counterpart, and that is the design: an appointment
/// references a service by identifier, this module cannot know who holds such
/// a reference, and a deleted row would leave those references pointing at
/// nothing. Identifiers are never reused.
/// </summary>
/// <param name="ServiceId">Service to withdraw.</param>
public sealed record ArchiveServiceCommand(Guid ServiceId) : IRequest;

/// <summary>Validation rules for <see cref="ArchiveServiceCommand"/>.</summary>
public sealed class ArchiveServiceCommandValidator : AbstractValidator<ArchiveServiceCommand>
{
    /// <summary>Initializes the rules.</summary>
    public ArchiveServiceCommandValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
    }
}

/// <summary>
/// Handles <see cref="ArchiveServiceCommand"/>.
/// </summary>
public sealed class ArchiveServiceCommandHandler : IRequestHandler<ArchiveServiceCommand>
{
    private readonly ServicesDbContext _dbContext;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="eventBus">Bus the archival event is published on.</param>
    public ArchiveServiceCommandHandler(ServicesDbContext dbContext, IEventBus eventBus)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Archives the service.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task Handle(ArchiveServiceCommand request, CancellationToken cancellationToken)
    {
        var service =
            await _dbContext
                .Services.SingleOrDefaultAsync(
                    entity => entity.Id == request.ServiceId,
                    cancellationToken
                )
                .ConfigureAwait(false) ?? throw new NotFoundException("Service", request.ServiceId);

        service.Archive();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Nothing to publish when it was already archived: the entity raises
        // the event only on the transition, so a repeated call is silent
        // rather than a duplicate announcement subscribers must deduplicate.
        await ServiceWriteOperations
            .PublishPendingAsync(service, _eventBus, cancellationToken)
            .ConfigureAwait(false);
    }
}
