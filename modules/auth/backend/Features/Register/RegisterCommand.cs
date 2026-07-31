using EnterpriseFramework.Modules.Auth.Contracts;
using FluentValidation;
using MediatR;

namespace EnterpriseFramework.Modules.Auth.Features.Register;

/// <summary>
/// Creates a new account and signs it in.
/// </summary>
/// <param name="Email">Email address; must be unique.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
/// <param name="Password">Raw password; hashed before persistence, never stored.</param>
public sealed record RegisterCommand(string Email, string DisplayName, string Password)
    : IRequest<AuthSession>;

/// <summary>
/// Validation rules for <see cref="RegisterCommand"/>, mirrored client-side by
/// the module frontend's schemas. The password policy is deliberately the
/// same as `passwordSchema` in @enterprise/shared.
/// </summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches("[a-z]")
            .WithMessage("Include a lowercase letter.")
            .Matches("[A-Z]")
            .WithMessage("Include an uppercase letter.")
            .Matches("[0-9]")
            .WithMessage("Include a digit.");
    }
}
