using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Users.Contracts;
using EnterpriseFramework.Modules.Users.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Users.Features.UpdateUser;

/// <summary>
/// Updates the editable fields of a user profile.
/// </summary>
/// <param name="UserId">Account whose profile is updated.</param>
/// <param name="DisplayName">New display name.</param>
/// <param name="JobTitle">New job title, or null to clear it.</param>
/// <param name="IsActive">Whether the account is active.</param>
public sealed record UpdateUserCommand(
    Guid UserId,
    string DisplayName,
    string? JobTitle,
    bool IsActive
) : IRequest<UserProfileDto>;

/// <summary>Validation rules for <see cref="UpdateUserCommand"/>.</summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>Initializes the rules.</summary>
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.JobTitle).MaximumLength(200);
    }
}

/// <summary>
/// Handles <see cref="UpdateUserCommand"/>.
/// </summary>
public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserProfileDto>
{
    private readonly UsersDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Users persistence.</param>
    public UpdateUserCommandHandler(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Applies the update.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated profile.</returns>
    /// <exception cref="NotFoundException">Thrown when the profile does not exist.</exception>
    public async Task<UserProfileDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken
    )
    {
        var profile =
            await _dbContext
                .Profiles.SingleOrDefaultAsync(p => p.Id == request.UserId, cancellationToken)
                .ConfigureAwait(false) ?? throw new NotFoundException("User", request.UserId);

        profile.Update(request.DisplayName, request.JobTitle);
        profile.SetActive(request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UserProfileDto.FromProfile(profile);
    }
}
