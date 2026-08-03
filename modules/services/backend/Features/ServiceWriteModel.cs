using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Services.Domain;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features;

/// <summary>
/// The editable fields of a service, as they travel in a create or edit
/// request.
///
/// One shape for both operations because the two forms are the same form:
/// splitting them would mean maintaining two validators that must agree, and
/// the day they stop agreeing is the day a rule holds on creation only.
/// </summary>
/// <param name="Title">Name of the service. Required.</param>
/// <param name="Slug">
/// URL segment of the public page. Leave it empty and it is derived from the
/// title, disambiguated if needed; provide one and it is used verbatim, or
/// rejected when already taken.
/// </param>
/// <param name="ShortDescription">One line, shown on the showcase cards.</param>
/// <param name="Description">Long text of the detail page.</param>
/// <param name="DurationMinutes">
/// Length of one appointment. Required when <paramref name="IsBookable"/> is
/// set, because it is what generates the bookable slots.
/// </param>
/// <param name="Price">Price, or null to publish no price at all.</param>
/// <param name="Currency">ISO 4217 code; required when a price is given.</param>
/// <param name="IsPublished">Whether visitors can see it.</param>
/// <param name="IsBookable">Whether it accepts bookings.</param>
/// <param name="SortOrder">Position in the showcase; lower comes first.</param>
public sealed record ServiceWriteModel(
    string Title,
    string? Slug,
    string ShortDescription,
    string Description,
    int? DurationMinutes,
    decimal? Price,
    string? Currency,
    bool IsPublished,
    bool IsBookable,
    int SortOrder
);

/// <summary>
/// Validation rules shared by the create and edit commands.
///
/// The two rules worth reading are the conditional ones: a bookable service
/// without a duration would offer a booking page with no slots on it, and a
/// price without a currency is a number nobody can act on. Both are stated
/// here, next to the field, so the form can show them where the mistake is.
/// </summary>
public sealed class ServiceWriteModelValidator : AbstractValidator<ServiceWriteModel>
{
    /// <summary>Initializes the rules.</summary>
    public ServiceWriteModelValidator()
    {
        RuleFor(model => model.Title).NotEmpty().MaximumLength(200);
        RuleFor(model => model.ShortDescription).NotEmpty().MaximumLength(300);
        RuleFor(model => model.Description).MaximumLength(8000);
        RuleFor(model => model.SortOrder).GreaterThanOrEqualTo(0);

        RuleFor(model => model.Slug)
            .MaximumLength(Slug.MaxLength)
            // Only when one was typed: an empty slug is the normal case and
            // means "derive it from the title".
            .Must(value => Slug.From(value) == value)
            .When(model => !string.IsNullOrWhiteSpace(model.Slug))
            .WithMessage(
                "The slug may only contain lowercase letters, digits and single hyphens."
            );

        RuleFor(model => model.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(24 * 60)
            .When(model => model.DurationMinutes is not null);

        RuleFor(model => model.DurationMinutes)
            .NotNull()
            .When(model => model.IsBookable)
            .WithMessage("A bookable service needs a duration: without one there are no slots.");

        RuleFor(model => model.Price)
            .GreaterThanOrEqualTo(0)
            .When(model => model.Price is not null);

        RuleFor(model => model.Currency)
            .NotEmpty()
            .Length(3)
            .When(model => model.Price is not null)
            .WithMessage("A price needs an ISO 4217 currency code, for example EUR.");
    }
}

/// <summary>
/// Steps the create and edit handlers share: resolving the slug against the
/// catalogue, and announcing what changed.
///
/// It exists so the two handlers cannot answer "is this slug free?"
/// differently — the question has one right answer and it involves the whole
/// table, archived rows included.
/// </summary>
internal static class ServiceWriteOperations
{
    /// <summary>
    /// Decides the slug a service will carry.
    ///
    /// A slug the administrator typed is honoured or refused, never altered.
    /// A slug derived from the title is disambiguated, because the
    /// administrator did not ask for that address and would rather have a
    /// working page than an error.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="requested">Slug from the request, possibly empty.</param>
    /// <param name="title">Title to derive from when no slug was given.</param>
    /// <param name="excludedServiceId">
    /// Service being edited, excluded from the collision check so re-saving a
    /// form without touching the slug does not report a clash with itself.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A slug that is free within the catalogue.</returns>
    /// <exception cref="Application.Exceptions.BusinessException">
    /// Thrown when the title yields no usable slug, or when an explicitly
    /// requested slug is already taken.
    /// </exception>
    public static async Task<string> ResolveSlugAsync(
        ServicesDbContext dbContext,
        string? requested,
        string title,
        Guid? excludedServiceId,
        CancellationToken cancellationToken
    )
    {
        var wasRequested = !string.IsNullOrWhiteSpace(requested);
        var desired = Slug.From(wasRequested ? requested : title);

        if (desired.Length == 0)
        {
            throw new Application.Exceptions.BusinessException(
                "This title produces no usable address. Give the service a slug of its own."
            );
        }

        // Only the slugs that could possibly collide, not the whole table:
        // "consulenza" can only clash with itself or with "consulenza-<n>".
        var neighbours = await dbContext
            .Services.AsNoTracking()
            .Where(service =>
                service.Id != excludedServiceId && service.Slug.StartsWith(desired)
            )
            .Select(service => service.Slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (wasRequested)
        {
            if (neighbours.Contains(desired, StringComparer.Ordinal))
            {
                throw new Application.Exceptions.BusinessException(
                    $"The address '{desired}' is already used by another service."
                );
            }

            return desired;
        }

        return Slug.Disambiguate(desired, neighbours);
    }

    /// <summary>
    /// Publishes the events the entity raised and clears them.
    ///
    /// After persistence, never before: an event announcing a change that then
    /// failed to save is a lie other modules would act on.
    /// </summary>
    /// <param name="entity">Entity whose events are pending.</param>
    /// <param name="eventBus">Bus the events are published on.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public static async Task PublishPendingAsync(
        EntityBase<Guid> entity,
        IEventBus eventBus,
        CancellationToken cancellationToken
    )
    {
        foreach (var domainEvent in entity.DomainEvents)
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        entity.ClearDomainEvents();
    }
}
