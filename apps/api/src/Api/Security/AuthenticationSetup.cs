using System.Text;
using EnterpriseFramework.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseFramework.Api.Security;

/// <summary>
/// Host-side authentication and authorization.
///
/// The HOST owns token VALIDATION and the authorization pipeline — they must
/// exist even when the Auth module is disabled (endpoints then simply have no
/// way to obtain a token). The Auth module owns token ISSUANCE. Both read the
/// same "Jwt" configuration section, so issued tokens always validate.
/// </summary>
public static class AuthenticationSetup
{
    /// <summary>
    /// Adds JWT bearer authentication and the secure-by-default authorization
    /// policy.
    /// </summary>
    /// <param name="services">Service collection to register into.</param>
    /// <param name="configuration">Application configuration (section "Jwt").</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var signingKey = configuration["Jwt:SigningKey"] ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "enterprise-framework",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "enterprise-framework",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            // A placeholder keeps startup possible with the Auth
                            // module disabled; it can never validate a real token.
                            signingKey.Length > 0 ? signingKey : new string('!', 64)
                        )
                    ),
                    // Default is 5 minutes: added to a 15-minute token it
                    // extends real lifetime by a third.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        // SECURE BY DEFAULT: every endpoint requires an authenticated caller
        // unless it explicitly opts out with AllowAnonymous. Forgetting to
        // protect a new endpoint therefore fails closed, not open.
        services.AddAuthorization(options =>
            options.FallbackPolicy = options.DefaultPolicy
        );

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
