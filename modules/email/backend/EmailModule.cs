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
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddSingleton<EmailOutbox>();
        services.AddSingleton<IEmailOutbox>(provider => provider.GetRequiredService<EmailOutbox>());
        services.AddHostedService<EmailBackgroundSender>();

        // The transport is chosen by CONFIGURATION, not by code: a host in
        // "Email:Smtp" means deliver, its absence means log. That way turning
        // real email on is a deployment concern, and forgetting to configure
        // it fails SAFE — a developer running against a seeded database
        // cannot email real people by accident.
        //
        // A project needing a transactional provider still registers its own
        // IEmailSender after this module; the last registration wins.
        var smtp = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>();

        if (smtp?.IsConfigured == true)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EmailModule).Assembly));
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Intentionally empty: see the class summary.
    }
}
