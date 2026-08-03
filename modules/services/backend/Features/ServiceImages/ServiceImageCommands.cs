using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Services.Contracts;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features.ServiceImages;

/// <summary>
/// Attaches an already-uploaded file to a service.
///
/// The upload itself belongs to the Storage module: this module never sees
/// bytes, it records that a file identifier illustrates a service and what it
/// depicts. The file must have been uploaded as PUBLIC, or the showcase would
/// ask an anonymous visitor for a token.
/// </summary>
/// <param name="ServiceId">Service the image belongs to.</param>
/// <param name="StorageFileId">Identifier returned by the Storage upload.</param>
/// <param name="AltText">Text alternative. Required.</param>
/// <param name="SortOrder">Position in the gallery.</param>
public sealed record AddServiceImageCommand(
    Guid ServiceId,
    Guid StorageFileId,
    string AltText,
    int SortOrder
) : IRequest<ServiceImageDto>;

/// <summary>
/// Validation rules for <see cref="AddServiceImageCommand"/>.
///
/// The alt text is required here and non-nullable in the model: the
/// accessibility contract of the design system is not a suggestion, and a
/// field that may be skipped is a field that always is.
/// </summary>
public sealed class AddServiceImageCommandValidator : AbstractValidator<AddServiceImageCommand>
{
    /// <summary>Initializes the rules.</summary>
    public AddServiceImageCommandValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StorageFileId).NotEmpty();
        RuleFor(command => command.AltText)
            .NotEmpty()
            .MaximumLength(300)
            .WithMessage("Describe the image: without a text alternative it is invisible to some visitors.");
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Handles <see cref="AddServiceImageCommand"/>.
/// </summary>
public sealed class AddServiceImageCommandHandler
    : IRequestHandler<AddServiceImageCommand, ServiceImageDto>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public AddServiceImageCommandHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Attaches the image.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The attached image.</returns>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    /// <exception cref="BusinessException">Thrown when the service is archived.</exception>
    public async Task<ServiceImageDto> Handle(
        AddServiceImageCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await LoadAsync(_dbContext, request.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var image = service.AddImage(request.StorageFileId, request.AltText, request.SortOrder);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServiceImageDto.FromImage(image);
    }

    /// <summary>
    /// Loads a service with its gallery, refusing archived ones.
    /// Shared by the three image commands so they cannot disagree on it.
    /// </summary>
    internal static async Task<Domain.Service> LoadAsync(
        ServicesDbContext dbContext,
        Guid serviceId,
        CancellationToken cancellationToken
    )
    {
        var service =
            await dbContext
                .Services.Include(entity => entity.Images)
                .SingleOrDefaultAsync(entity => entity.Id == serviceId, cancellationToken)
                .ConfigureAwait(false) ?? throw new NotFoundException("Service", serviceId);

        if (service.IsArchived)
        {
            throw new BusinessException("An archived service cannot be edited.");
        }

        return service;
    }
}

/// <summary>
/// Detaches an image from a service.
///
/// The FILE is not deleted: it belongs to the Storage module, which owns its
/// lifecycle, and a file may well be reused elsewhere. Deleting bytes from
/// here would be one module reaching into another's responsibility.
/// </summary>
/// <param name="ServiceId">Service the image belongs to.</param>
/// <param name="ImageId">Image to detach.</param>
public sealed record RemoveServiceImageCommand(Guid ServiceId, Guid ImageId) : IRequest;

/// <summary>
/// Handles <see cref="RemoveServiceImageCommand"/>.
/// </summary>
public sealed class RemoveServiceImageCommandHandler : IRequestHandler<RemoveServiceImageCommand>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public RemoveServiceImageCommandHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Detaches the image.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    /// <exception cref="BusinessException">Thrown when the service is archived.</exception>
    public async Task Handle(
        RemoveServiceImageCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await AddServiceImageCommandHandler
            .LoadAsync(_dbContext, request.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        // Idempotent: an image that is not there satisfies the caller's goal.
        service.RemoveImage(request.ImageId);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Chooses which image represents a service on cards and in link previews.
/// </summary>
/// <param name="ServiceId">Service the image belongs to.</param>
/// <param name="ImageId">Image to promote.</param>
public sealed record SetServiceCoverCommand(Guid ServiceId, Guid ImageId) : IRequest;

/// <summary>
/// Handles <see cref="SetServiceCoverCommand"/>.
/// </summary>
public sealed class SetServiceCoverCommandHandler : IRequestHandler<SetServiceCoverCommand>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public SetServiceCoverCommandHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Promotes the image to cover.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">
    /// Thrown when the service does not exist, or when the image is not one of
    /// its own.
    /// </exception>
    /// <exception cref="BusinessException">Thrown when the service is archived.</exception>
    public async Task Handle(SetServiceCoverCommand request, CancellationToken cancellationToken)
    {
        var service = await AddServiceImageCommandHandler
            .LoadAsync(_dbContext, request.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        if (!service.SetCover(request.ImageId))
        {
            throw new NotFoundException("ServiceImage", request.ImageId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
