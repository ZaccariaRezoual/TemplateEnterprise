using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Services.Contracts;
using EnterpriseFramework.Modules.Services.Domain;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features.UpdateService;

/// <summary>
/// Edits a service of the catalogue.
/// </summary>
/// <param name="ServiceId">Service to edit.</param>
/// <param name="Model">The editable fields, as the form submitted them.</param>
public sealed record UpdateServiceCommand(Guid ServiceId, ServiceWriteModel Model)
    : IRequest<AdminServiceDto>;

/// <summary>
/// Validation rules for <see cref="UpdateServiceCommand"/>; the field rules
/// themselves live in <see cref="ServiceWriteModelValidator"/>, shared with
/// the create command.
/// </summary>
public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    /// <summary>Initializes the rules.</summary>
    public UpdateServiceCommandValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.Model).NotNull().SetValidator(new ServiceWriteModelValidator());
    }
}

/// <summary>
/// Handles <see cref="UpdateServiceCommand"/>.
///
/// Responsibilities: load the service with its gallery, refuse to edit an
/// archived one, resolve the slug, persist, then publish what the entity
/// decided to announce.
/// </summary>
public sealed class UpdateServiceCommandHandler
    : IRequestHandler<UpdateServiceCommand, AdminServiceDto>
{
    private readonly ServicesDbContext _dbContext;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="eventBus">Bus the public events are published on.</param>
    public UpdateServiceCommandHandler(ServicesDbContext dbContext, IEventBus eventBus)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Applies the edit.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated service.</returns>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when the service is archived, when the title yields no usable
    /// slug, or when the requested slug is already taken.
    /// </exception>
    public async Task<AdminServiceDto> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var service =
            await _dbContext
                .Services.Include(entity => entity.Images)
                .SingleOrDefaultAsync(entity => entity.Id == request.ServiceId, cancellationToken)
                .ConfigureAwait(false) ?? throw new NotFoundException("Service", request.ServiceId);

        // Archiving is how a service is retired, and a retired service that
        // could still be edited back into the showcase would make the state
        // meaningless. Un-archiving is deliberately not offered: the way back
        // is a new service, because the old identifier may already be
        // referenced by appointments that must not change meaning.
        if (service.IsArchived)
        {
            throw new BusinessException("An archived service cannot be edited.");
        }

        var model = request.Model;

        var slug = await ServiceWriteOperations
            .ResolveSlugAsync(
                _dbContext,
                model.Slug,
                model.Title,
                service.Id,
                cancellationToken
            )
            .ConfigureAwait(false);

        service.Update(
            new ServiceState(
                model.Title,
                slug,
                model.ShortDescription,
                model.Description,
                model.DurationMinutes,
                model.Price,
                model.Currency,
                model.IsPublished,
                model.IsBookable,
                model.SortOrder
            )
        );

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await ServiceWriteOperations
            .PublishPendingAsync(service, _eventBus, cancellationToken)
            .ConfigureAwait(false);

        return AdminServiceDto.FromService(service);
    }
}
