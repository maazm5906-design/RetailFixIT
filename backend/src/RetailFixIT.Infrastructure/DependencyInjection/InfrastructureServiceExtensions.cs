using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;
using RetailFixIT.Infrastructure.AI;
using RetailFixIT.Infrastructure.Auth;
using RetailFixIT.Infrastructure.Caching;
using RetailFixIT.Infrastructure.Messaging.Consumers;
using RetailFixIT.Infrastructure.Persistence;
using RetailFixIT.Infrastructure.Persistence.Repositories;
using RetailFixIT.Infrastructure.Realtime;
using RetailFixIT.Infrastructure.Services;
using System.Text;

namespace RetailFixIT.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // HTTP context and current user/tenant (scoped per request)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();

        // Database — EF Core with Azure Cosmos DB
        services.AddDbContext<AppDbContext>(options =>
            options.UseCosmos(
                config["CosmosDb:Endpoint"]!,
                config["CosmosDb:AccountKey"]!,
                config["CosmosDb:DatabaseName"] ?? "RetailFixIT"));

        // Repositories
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAIRecommendationRepository, AIRecommendationRepository>();

        // Cache — swap provider via config
        var cacheProvider = config["Cache:Provider"] ?? "Memory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = config["Cache:Redis:ConnectionString"]);
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        // AI provider — swap via config (Gemini dev, AzureOpenAI prod)
        services.AddHttpClient("Gemini");
        services.AddHttpClient("AzureOpenAI");
        var aiProvider = config["AI:Provider"] ?? "Gemini";
        if (aiProvider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IAIProvider, AzureOpenAIProvider>();
        else
            services.AddScoped<IAIProvider, GeminiAIProvider>();

        // Auth — swap provider via config
        var authProvider = config["Auth:Provider"] ?? "Jwt";
        if (authProvider.Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
        {
            // Azure AD: validate tokens issued by Entra ID via Microsoft.Identity.Web
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(config, configSectionName: "Auth:AzureAd");

            // SignalR: also accept token from query string (same as dev path)
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, opts =>
                {
                    var prev = opts.Events?.OnMessageReceived;
                    opts.Events ??= new JwtBearerEvents();
                    opts.Events.OnMessageReceived = async ctx =>
                    {
                        if (prev != null) await prev(ctx);
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                    };
                });
        }
        else
        {
            // Development: local JWT Bearer
            var key = Encoding.UTF8.GetBytes(config["Auth:Jwt:SecretKey"] ?? "dev-secret-key-minimum-32-chars!!");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Auth:Jwt:Issuer"],
                        ValidAudience = config["Auth:Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                    // Allow JWT from SignalR query string
                    opts.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var token = ctx.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) &&
                                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                                ctx.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                });
        }
        // Always register JwtTokenService — AuthController injects it for the dev /login endpoint
        services.AddScoped<JwtTokenService>();
        services.AddAuthorization();

        // MassTransit — in-memory (dev) or Azure Service Bus (prod)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<JobCreatedConsumer>();
            x.AddConsumer<AIRecommendationRequestedConsumer>();
            x.AddConsumer<AIRecommendationGeneratedConsumer>();
            x.AddConsumer<JobAssignedConsumer>();

            var messagingProvider = config["Messaging:Provider"] ?? "InMemory";
            if (messagingProvider.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    cfg.Host(config["Messaging:AzureServiceBus:ConnectionString"]);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingInMemory((ctx, cfg) =>
                {
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        // SignalR — in-process (dev) or Azure SignalR Service (prod)
        var signalRBuilder = services.AddSignalR();
        var signalRProvider = config["SignalR:Provider"] ?? "InMemory";
        if (signalRProvider.Equals("AzureSignalR", StringComparison.OrdinalIgnoreCase))
        {
            signalRBuilder.AddAzureSignalR(config["SignalR:AzureSignalR:ConnectionString"]);
        }

        services.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();

        return services;
    }
}
