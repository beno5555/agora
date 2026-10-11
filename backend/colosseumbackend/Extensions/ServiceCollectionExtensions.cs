using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using colosseumbackend.BackgroundServices;
using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Data;
using colosseumbackend.Logging;
using colosseumbackend.Middleware;
using colosseumbackend.Repositories;
using colosseumbackend.Services;
using colosseumbackend.Solana;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Solnet.Rpc;
using Solnet.Wallet;

namespace colosseumbackend.Extensions;

public static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "Agora";

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SolanaOptions>(configuration.GetSection(SolanaOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<DealSyncOptions>(configuration.GetSection(DealSyncOptions.SectionName));
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AgoraDbContext>(options => options.UseSqlite(configuration.GetConnectionString(ConnectionStringName)));

        services.AddScoped<DatabaseLogger>();
        services.AddScoped<UserRepository>();
        services.AddScoped<AuthNonceRepository>();
        services.AddScoped<GameRepository>();
        services.AddScoped<ListingRepository>();
        services.AddScoped<DealRepository>();
        services.AddScoped<NotificationRepository>();
        services.AddScoped<ActionLogRepository>();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<GameService>();
        services.AddScoped<ListingService>();
        services.AddScoped<DealSyncService>();
        services.AddScoped<DealService>();
        services.AddScoped<NotificationService>();

        services.AddHostedService<DealSyncHostedService>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }

    public static IServiceCollection AddSolana(this IServiceCollection services)
    {
        services.AddSingleton<IRpcClient>(provider => ClientFactory.GetClient(provider.GetRequiredService<IOptions<SolanaOptions>>().Value.RpcUrl));
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SolanaOptions>>().Value;
            return new AgoraProgram(new PublicKey(options.ProgramId), new PublicKey(options.Mint));
        });
        services.AddSingleton<SolanaGateway>();
        return services;
    }

    /// <summary>
    /// Session JWT read from the HttpOnly cookie. In Development, a missing signing key is replaced
    /// by a random one (sessions then end when the app restarts); elsewhere it's required.
    /// </summary>
    public static IServiceCollection AddWalletAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        bool hasKey = !string.IsNullOrWhiteSpace(jwtOptions.SigningKey);

        if (!hasKey && environment.IsDevelopment())
        {
            jwtOptions.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            services.PostConfigure<JwtOptions>(options => options.SigningKey = jwtOptions.SigningKey);
        }
        else if (!hasKey)
        {
            throw new InvalidOperationException("Jwt:SigningKey is not configured");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer      = jwtOptions.Issuer,
                ValidAudience    = jwtOptions.Audience,
                IssuerSigningKey = JwtTokenService.SigningKey(jwtOptions),
                NameClaimType    = ClaimTypes.NameIdentifier,
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies[Constants.SessionCookieName];
                    return Task.CompletedTask;
                },
            };
        });
        services.AddAuthorization();
        return services;
    }

    /// <summary>Signed-in callers are limited per wallet, everyone else per IP; a global limiter backs both.</summary>
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitConfig.Policies.Auth,  context => FixedWindow(context, RateLimitConfig.Limits.AuthPerMinute));
            options.AddPolicy(RateLimitConfig.Policies.Read,  context => FixedWindow(context, RateLimitConfig.Limits.ReadPerMinute));
            options.AddPolicy(RateLimitConfig.Policies.Write, context => FixedWindow(context, RateLimitConfig.Limits.WritePerMinute));
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => FixedWindow(context, RateLimitConfig.Limits.GlobalPerMinute));
        });
        return services;
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permitsPerMinute)
    {
        string partitionKey = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.Connection.RemoteIpAddress?.ToString()
                           ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window      = TimeSpan.FromMinutes(1),
        });
    }
}
