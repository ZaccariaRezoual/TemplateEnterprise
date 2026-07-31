using FluentValidation;

namespace EnterpriseFramework.Modules.Demo.Features.Echo;

/// <summary>
/// Validation rules for <see cref="EchoCommand"/>, executed automatically by
/// the framework's MediatR validation behavior before the handler runs.
/// </summary>
public sealed class EchoCommandValidator : AbstractValidator<EchoCommand>
{
    /// <summary>
    /// Initializes the rules: Text is required and at most 500 characters.
    /// </summary>
    public EchoCommandValidator()
    {
        RuleFor(command => command.Text).NotEmpty().MaximumLength(500);
    }
}
