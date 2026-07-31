using System.Threading.Channels;
using EnterpriseFramework.Modules.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Email.Services;

/// <summary>
/// In-process outbox backed by an unbounded channel.
///
/// It decouples request handling from mail delivery, but it is deliberately
/// NOT durable: pending messages are lost if the process stops. That is an
/// acceptable trade for a welcome email and a terrible one for an invoice, so
/// projects with delivery guarantees swap this for the Background Jobs
/// infrastructure of Fase 7 (a persisted queue). The interface stays the
/// same, so callers do not change.
/// </summary>
public sealed class EmailOutbox : IEmailOutbox
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    /// <summary>Reader consumed by the background sender.</summary>
    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    /// <inheritdoc />
    public void Add(EmailMessage message) => _channel.Writer.TryWrite(message);
}

/// <summary>
/// Background service that drains the outbox and delivers each message.
///
/// A delivery failure is logged and the message dropped rather than retried
/// forever: an endlessly retried bad address would block everything behind
/// it. Retry policy belongs to a durable job system, not here.
/// </summary>
public sealed partial class EmailBackgroundSender : BackgroundService
{
    private readonly EmailOutbox _outbox;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailBackgroundSender> _logger;

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not deliver email to {Recipient}")]
    private static partial void LogDeliveryFailure(
        ILogger logger,
        string recipient,
        Exception exception
    );

    /// <summary>
    /// Initializes the sender.
    /// </summary>
    /// <param name="outbox">Outbox to drain.</param>
    /// <param name="services">Root provider used to resolve a scoped sender per message.</param>
    /// <param name="logger">Reports delivery failures.</param>
    public EmailBackgroundSender(
        EmailOutbox outbox,
        IServiceProvider services,
        ILogger<EmailBackgroundSender> logger
    )
    {
        _outbox = outbox;
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _outbox.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(message, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogDeliveryFailure(_logger, message.To, exception);
            }
        }
    }
}
