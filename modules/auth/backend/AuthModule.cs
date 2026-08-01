using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Features.Login;
using EnterpriseFramework.Modules.Auth.Features.Logout;
using EnterpriseFramework.Modules.Auth.Features.Profile;
using EnterpriseFramework.Modules.Auth.Features.Refresh;
using EnterpriseFramework.Modules.Auth.Features.Register;
using EnterpriseFramework.Modules.Auth.Options;
using EnterpriseFramework.Modules.Auth.Persistence;
using EnterpriseFramework.Modules.Auth.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace EnterpriseFramework.Modules.Auth;

/// <summary>
/// Auth module: registration, login, refresh-token rotation and logout.
///
/// THE canonical module template. It demonstrates every part of the Module
/// Contract: module-owned persistence (schema "auth" + own migrations), typed
/// options, MediatR features with validators, domain events on the bus, a
/// module-scoped rate-limiting policy and endpoint mapping with cookie
/// handling kept OUT of the handlers.
/// </summary>
public sealed class AuthModule : IModule
{
    /// <summary>Name of the refresh-token cookie.</summary>
    public const string RefreshCookieName = "ef_refresh";

    /// <summary>
    /// Rate-limiting policy for credential-guessing endpoints (login, register).
    /// Deliberately tight: these are the brute-force surface.
    /// </summary>
    public const string CredentialsRateLimitPolicy = "auth-credentials";

    /// <summary>
    /// Rate-limiting policy for token refresh.
    ///
    /// Separate from credentials on purpose: refresh is a legitimate
    /// high-frequency call (every page load, every open tab, every expiring
    /// token). Sharing the credential budget would lock out a normal user
    /// working with several tabs while doing nothing against an attacker,
    /// who needs a valid refresh cookie to reach it at all.
    /// </summary>
    public const string RefreshRateLimitPolicy = "auth-refresh";

    private const string CookiePath = "/api/auth";

    /// <inheritdoc />
    public string Name => "Auth";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AuthDbContext.Schema)
            )
        );

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<TokenService>();
        services.AddScoped<SessionFactory>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AuthModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        // Brute-force containment, partitioned per client IP and configurable
        // under "Modules:Auth" (development loosens it; production tunes it).
        var credentialsLimit = configuration.GetValue("Modules:Auth:CredentialsPermitLimit", 10);
        var refreshLimit = configuration.GetValue("Modules:Auth:RefreshPermitLimit", 60);
        var windowSeconds = configuration.GetValue("Modules:Auth:RateLimitWindowSeconds", 60);

        services.Configure<RateLimiterOptions>(options =>
        {
            options.AddPolicy(
                CredentialsRateLimitPolicy,
                context => PerClientIpWindow(context, credentialsLimit, windowSeconds)
            );
            options.AddPolicy(
                RefreshRateLimitPolicy,
                context => PerClientIpWindow(context, refreshLimit, windowSeconds)
            );
        });

        // Dev/test convenience: apply this module's migrations at startup.
        // Production deployments run migrations as an explicit release step
        // and set Modules:Auth:AutoMigrate to false.
        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<AuthDbMigrator>();
        }

        services
            .AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName));

        // Registered after the migrator so the schema exists when it runs:
        // hosted services start in registration order.
        if (configuration.GetValue($"{BootstrapAdminOptions.SectionName}:Enabled", false))
        {
            services.AddHostedService<BootstrapAdminSeeder>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        // Credential endpoints are anonymous by necessity and rate limited
        // strictly; everything else inherits the host's authenticated-only
        // fallback policy.
        group
            .MapPost(
                "/register",
                async (RegisterCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
                {
                    var session = await sender.Send(command, ct);
                    AppendRefreshCookie(http, session);
                    return TypedResults.Ok(session.Response);
                }
            )
            .WithName("authRegister")
            .WithSummary("Creates an account and signs it in.")
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimitPolicy)
            .ProducesValidationProblem();

        group
            .MapPost(
                "/login",
                async (LoginCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
                {
                    var session = await sender.Send(command, ct);
                    AppendRefreshCookie(http, session);
                    return TypedResults.Ok(session.Response);
                }
            )
            .WithName("authLogin")
            .WithSummary("Authenticates with email and password.")
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimitPolicy)
            .ProducesValidationProblem();

        group
            .MapPost(
                "/refresh",
                async (ISender sender, HttpContext http, CancellationToken ct) =>
                {
                    var raw = http.Request.Cookies[RefreshCookieName];
                    if (string.IsNullOrEmpty(raw))
                    {
                        return Results.Unauthorized();
                    }
                    var session = await sender.Send(new RefreshCommand(raw), ct);
                    AppendRefreshCookie(http, session);
                    return Results.Ok(session.Response);
                }
            )
            .WithName("authRefresh")
            .WithSummary("Rotates the refresh token and returns a new access token.")
            .AllowAnonymous()
            .RequireRateLimiting(RefreshRateLimitPolicy)
            .Produces<AuthResponse>();

        group
            .MapPost(
                "/logout",
                async (ISender sender, HttpContext http, CancellationToken ct) =>
                {
                    await sender.Send(new LogoutCommand(http.Request.Cookies[RefreshCookieName]), ct);
                    http.Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(http));
                    return TypedResults.NoContent();
                }
            )
            .WithName("authLogout")
            .WithSummary("Revokes the current session's refresh token.")
            .AllowAnonymous();

        group
            .MapGet(
                "/me",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetMeQuery(), ct))
            )
            .WithName("authMe")
            .WithSummary("Returns the authenticated caller's profile.");
    }

    /// <summary>
    /// Writes the refresh token into its httpOnly cookie. Session lifetime
    /// lives in the cookie, not in script-accessible storage: SameSite=Strict
    /// plus the /api/auth path keep it off cross-site requests and off every
    /// other endpoint.
    /// </summary>
    private static void AppendRefreshCookie(HttpContext http, AuthSession session)
    {
        var tokenService = http.RequestServices.GetRequiredService<TokenService>();
        var options = BuildCookieOptions(http);
        options.Expires = DateTimeOffset.UtcNow.Add(tokenService.RefreshTokenLifetime);
        http.Response.Cookies.Append(RefreshCookieName, session.RawRefreshToken, options);
    }

    /// <summary>
    /// Builds a fixed-window limiter partitioned by client IP, so one abusive
    /// client cannot consume everyone else's budget.
    /// </summary>
    private static RateLimitPartition<string> PerClientIpWindow(
        HttpContext context,
        int permitLimit,
        int windowSeconds
    ) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            }
        );

    private static CookieOptions BuildCookieOptions(HttpContext http) =>
        new()
        {
            HttpOnly = true,
            // Secure requires HTTPS; local development runs plain HTTP.
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        };
}
