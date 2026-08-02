using System.Threading.RateLimiting;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Site.Features.Contact;
using EnterpriseFramework.Modules.Site.Options;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Site;

/// <summary>
/// Site module: the backend of the public site.
///
/// Today that is one endpoint — the contact form — and it is the most
/// exposed surface of the whole API: anonymous, and it accepts free text.
/// Everything unusual about this module follows from that.
///
/// It has no persistence: a message is published on the event bus and
/// delivered by whichever module handles email. Storing messages would mean a
/// schema, a screen to read them and a permission to open it — a small CRM,
/// which a project should add when it knows it needs one.
/// </summary>
public sealed class SiteModule : IModule
{
    /// <summary>
    /// Rate-limiting policy of the contact endpoint.
    ///
    /// Its own policy, far tighter than the global one: a human writes one
    /// message, not five a minute, and this is the only anonymous endpoint
    /// that accepts free text.
    /// </summary>
    public const string ContactRateLimitPolicy = "site-contact";

    /// <inheritdoc />
    public string Name => "Site";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SiteOptions>().Bind(configuration.GetSection(SiteOptions.SectionName));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SiteModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(SiteModule).Assembly);

        var options =
            configuration.GetSection(SiteOptions.SectionName).Get<SiteOptions>() ?? new SiteOptions();

        services.Configure<RateLimiterOptions>(limiter =>
            limiter.AddPolicy(
                ContactRateLimitPolicy,
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        // Per client IP: one abusive sender must not consume
                        // everyone else's budget.
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.ContactPermitLimit,
                            Window = TimeSpan.FromSeconds(options.ContactRateLimitWindowSeconds),
                            QueueLimit = 0,
                        }
                    )
            )
        );
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost(
                "/api/site/contact",
                async (SendContactMessageCommand command, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(command, ct);
                    return TypedResults.Accepted((string?)null);
                }
            )
            .WithName("siteContact")
            .WithSummary("Accepts a message from the public contact form.")
            .WithTags("Site")
            // Anonymous by necessity: the whole point is that a visitor with
            // no account can write. The rate-limiting policy above and the
            // honeypot on the command are what make that safe to expose.
            .AllowAnonymous()
            .RequireRateLimiting(ContactRateLimitPolicy)
            .ProducesValidationProblem();
    }
}
