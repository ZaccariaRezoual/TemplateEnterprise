using FluentValidation;
using MediatR;

namespace EnterpriseFramework.Modules.Site.Features.Contact;

/// <summary>
/// A message written by an anonymous visitor on the public contact form.
/// </summary>
/// <param name="Name">Name the visitor typed.</param>
/// <param name="Email">Address to reply to.</param>
/// <param name="Body">The message itself.</param>
/// <param name="Website">
/// Honeypot. A real visitor never sees this field and therefore leaves it
/// empty; automated submitters fill every input they find. When it carries a
/// value the submission is dropped and still answered with success — telling
/// a bot it was detected only teaches whoever wrote it to try again.
///
/// A honeypot instead of a captcha: no third-party dependency, no cognitive
/// load for the visitor, and it stops the bulk of automated traffic. A captcha
/// is a reasonable next step if the logs ever show it is needed — not before.
/// </param>
public sealed record SendContactMessageCommand(
    string Name,
    string Email,
    string Body,
    string? Website = null
) : IRequest;

/// <summary>
/// Validation rules for <see cref="SendContactMessageCommand"/>, mirrored
/// client-side by the Site module's Zod schema.
/// </summary>
public sealed class SendContactMessageCommandValidator
    : AbstractValidator<SendContactMessageCommand>
{
    /// <summary>Initializes the rules.</summary>
    public SendContactMessageCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(320);

        // An upper bound belongs here as much as a lower one: the endpoint is
        // anonymous, and without it a single request can post a megabyte.
        RuleFor(command => command.Body).NotEmpty().MaximumLength(5000);
    }
}
