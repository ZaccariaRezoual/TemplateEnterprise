using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Services.Contracts;
using EnterpriseFramework.Modules.Services.Domain;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using MediatR;

namespace EnterpriseFramework.Modules.Services.Features.CreateService;

/// <summary>
/// Adds a service to the catalogue.
/// </summary>
/// <param name="Model">The editable fields of the new service.</param>
public sealed record CreateServiceCommand(ServiceWriteModel Model) : IRequest<AdminServiceDto>;

/// <summary>
/// Validation rules for <see cref="CreateServiceCommand"/>; the field rules
/// themselves live in <see cref="ServiceWriteModelValidator"/>, shared with
/// the edit command.
/// </summary>
public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CreateServiceCommandValidator()
    {
        RuleFor(command => command.Model).NotNull().SetValidator(new ServiceWriteModelValidator());
    }
}

/// <summary>
/// Handles <see cref="CreateServiceCommand"/>.
///
/// Responsibilities: resolve the slug against the catalogue, persist the
/// service, then publish whatever the entity decided to announce. It does not
/// decide WHICH event that is — the entity does, because the answer depends
/// on the transition.
/// </summary>
public sealed class CreateServiceCommandHandler
    : IRequestHandler<CreateServiceCommand, AdminServiceDto>
{
    private readonly ServicesDbContext _dbContext;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="eventBus">Bus the public events are published on.</param>
    public CreateServiceCommandHandler(ServicesDbContext dbContext, IEventBus eventBus)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created service.</returns>
    /// <exception cref="Application.Exceptions.BusinessException">
    /// Thrown when the title yields no usable slug, or when the requested slug
    /// is already taken.
    /// </exception>
    public async Task<AdminServiceDto> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var model = request.Model;

        var slug = await ServiceWriteOperations
            .ResolveSlugAsync(_dbContext, model.Slug, model.Title, null, cancellationToken)
            .ConfigureAwait(false);

        var service = Service.Create(
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

        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await ServiceWriteOperations
            .PublishPendingAsync(service, _eventBus, cancellationToken)
            .ConfigureAwait(false);

        return AdminServiceDto.FromService(service);
    }
}
