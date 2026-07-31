using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Email.Abstractions;
using EnterpriseFramework.Modules.Email.Options;
using EnterpriseFramework.Modules.Email.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Email;

/// <summary>
/// Email module: templated outbound email with a swappable transport.
///
/// It exposes NO endpoints — sending email is never a client-triggered
/// operation, or the API becomes an open relay for spam. Other modules add
/// messages to the outbox, or this module reacts to their events (as it does
/// for registration).
///
/// A project replaces the transport by registering its own
/// <see cref="IEmailSender"/> after this module: the last registration wins.
/// </summary>
public sealed class EmailModule : IModule
{
    /// <inheritdoc />
    public string Name => "Email";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton<EmailOutbox>();
        services.AddSingleton<IEmailOutbox>(provider => provider.GetRequiredService<EmailOutbox>());
        services.AddHostedService<EmailBackgroundSender>();

        // Development default: log instead of delivering. Projects override
        // this with SMTP or a transactional provider per environment.
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EmailModule).Assembly));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Intentionally empty: see the class summary.
    }
}
