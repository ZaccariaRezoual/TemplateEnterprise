using EnterpriseFramework.Modules.Email.Abstractions;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Email.Services;

/// <summary>
/// Default <see cref="IEmailSender"/>: writes the message to the log instead
/// of delivering it.
///
/// It is the DEVELOPMENT default on purpose. A template that ships with SMTP
/// wired to a real server risks a developer emailing production users from a
/// seeded database; logging makes the content inspectable in Seq and harmless.
/// Configure a real provider per environment.
/// </summary>
public sealed partial class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email not sent (logging sender): to {Recipient}, subject {Subject}"
    )]
    private static partial void LogEmail(ILogger logger, string recipient, string subject);

    /// <summary>
    /// Initializes the sender.
    /// </summary>
    /// <param name="logger">Logger receiving the message.</param>
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogEmail(_logger, message.To, message.Subject);
        return Task.CompletedTask;
    }
}
