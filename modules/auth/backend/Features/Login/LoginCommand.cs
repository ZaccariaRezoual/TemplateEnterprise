using EnterpriseFramework.Modules.Auth.Contracts;
using FluentValidation;
using MediatR;

namespace EnterpriseFramework.Modules.Auth.Features.Login;

/// <summary>
/// Authenticates an account with email and password.
/// </summary>
/// <param name="Email">Email address of the account.</param>
/// <param name="Password">Raw password to verify.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthSession>;

/// <summary>
/// Validation rules for <see cref="LoginCommand"/>. Presence only: password
/// POLICY is not enforced at login, or accounts created before a policy
/// change could never sign in again.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes the rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty();
    }
}
