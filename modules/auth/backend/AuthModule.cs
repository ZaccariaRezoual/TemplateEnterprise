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

    /// <summary>Rate-limiting policy applied to credential endpoints.</summary>
    public const string SensitiveRateLimitPolicy = "auth-sensitive";

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
        services.AddSingleton<TokenService>();
        services.AddScoped<SessionFactory>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AuthModule).Assembly));
        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        // Brute-force containment: credential endpoints get a much smaller
        // budget than the global limiter, still partitioned per client IP.
        // Configurable under "Modules:Auth" (tests raise it; production tunes it).
        var permitLimit = configuration.GetValue("Modules:Auth:SensitivePermitLimit", 10);
        var windowSeconds = configuration.GetValue("Modules:Auth:SensitiveWindowSeconds", 60);
        services.Configure<RateLimiterOptions>(options =>
            options.AddPolicy(
                SensitiveRateLimitPolicy,
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            QueueLimit = 0,
                        }
                    )
            )
        );

        // Dev/test convenience: apply this module's migrations at startup.
        // Production deployments run migrations as an explicit release step
        // and set Modules:Auth:AutoMigrate to false.
        if (configuration.GetValue("Modules:Auth:AutoMigrate", defaultValue: true))
        {
            services.AddHostedService<AuthDbMigrator>();
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
            .RequireRateLimiting(SensitiveRateLimitPolicy)
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
            .RequireRateLimiting(SensitiveRateLimitPolicy)
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
            .RequireRateLimiting(SensitiveRateLimitPolicy)
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
