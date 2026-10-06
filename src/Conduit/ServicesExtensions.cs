using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Conduit.Features.Profiles;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Details = Conduit.Features.Users.Details;

namespace Conduit;

public static class ServicesExtensions
{
    public static void AddConduit(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors =
            [
                typeof(ValidationPipelineBehavior<,>),
                typeof(DBContextTransactionPipelineBehavior<,>),
            ];
        });

        services.AddValidatorsFromAssemblyContaining<Details.QueryValidator>();

        services.AddSingleton<Features.ConduitMapper>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IProfileReader, ProfileReader>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    }

    public static void AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuration["Jwt:SigningKey"] ?? "");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be a base64-encoded random key of at least 32 bytes."
            );
        }
        if (
            key.Length < 32
            || key.Distinct().Count() < 16
            || Encoding
                .UTF8.GetString(key)
                .Contains("somethinglongerforthisdumbalgorithmisrequired", StringComparison.Ordinal)
            || HasRepeatedPattern(key)
        )
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be a base64-encoded random key of at least 32 bytes; default and placeholder keys are not allowed."
            );
        }
        var signingKey = new SymmetricSecurityKey(key);
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var issuer = "issuer";
        var audience = "audience";

        services.Configure<JwtIssuerOptions>(options =>
        {
            options.Issuer = issuer;
            options.Audience = audience;
            options.SigningCredentials = signingCredentials;
        });

        var tokenValidationParameters = new TokenValidationParameters
        {
            // The signing key must match!
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            IssuerSigningKey = signingCredentials.Key,
            // Validate the JWT Issuer (iss) claim
            ValidateIssuer = true,
            ValidIssuer = issuer,
            // Validate the JWT Audience (aud) claim
            ValidateAudience = true,
            ValidAudience = audience,
            // Validate the token expiry
            ValidateLifetime = true,
            // If you want to allow a certain amount of clock drift, set that here:
            ClockSkew = TimeSpan.Zero,
        };

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        var subject = identity?.FindFirst("sub");
                        if (
                            subject is null
                            || identity!.FindAll("sub").Count() != 1
                            || identity.FindAll("conduit_token_version").Count() != 1
                            || identity.FindFirst("conduit_token_version")?.Value != "2"
                            || !subject.Value.StartsWith("user:", StringComparison.Ordinal)
                            || !int.TryParse(
                                subject.Value[5..],
                                NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out var personId
                            )
                            || personId <= 0
                        )
                        {
                            context.Fail("Invalid user identity");
                            return;
                        }
                        var db =
                            context.HttpContext.RequestServices.GetRequiredService<ConduitContext>();
                        var username = await db
                            .Persons.Where(x => x.PersonId == personId)
                            .Select(x => x.Username)
                            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                        if (username is null)
                        {
                            context.Fail("User no longer exists");
                            return;
                        }
                        foreach (var claim in identity.FindAll(ClaimTypes.Name).ToArray())
                        {
                            identity.RemoveClaim(claim);
                        }
                        identity.AddClaim(new Claim(ClaimTypes.Name, username));
                    },
                    OnMessageReceived = (context) =>
                    {
                        var token = context.HttpContext.Request.Headers.Authorization.ToString();
                        if (token.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = token["Token ".Length..].Trim();
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = (context) =>
                    {
                        // the RealWorld spec expects a JSON body on 401 responses
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(
                            """{"errors":{"token":["is missing"]}}"""
                        );
                    },
                };
            });
    }

    private static bool HasRepeatedPattern(byte[] key)
    {
        for (var period = 1; period <= key.Length / 2; period++)
        {
            var isRepeated = true;
            for (var index = period; index < key.Length; index++)
            {
                if (key[index] == key[index % period])
                {
                    continue;
                }

                isRepeated = false;
                break;
            }

            if (isRepeated)
            {
                return true;
            }
        }

        return false;
    }

    public static void AddSerilogLogging(this ILoggerFactory loggerFactory)
    {
        // Attach the sink to the logger configuration
        var log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            //just for local debug
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {SourceContext} {Message}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code
            )
            .CreateLogger();

        loggerFactory.AddSerilog(log);
        Log.Logger = log;
    }
}
