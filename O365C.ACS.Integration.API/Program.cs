// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Azure.Identity;
using O365C.ACS.Integration.API.Interfaces;
using O365C.ACS.Integration.API.Services;
using O365C.ACS.Integration.API.Middleware;
using O365C.ACS.Integration.API.Extensions;
using O365C.ACS.Integration.API.Models.Settings;
using O365C.ACS.Integration.API.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // Add global exception handling middleware
        builder.UseMiddleware<GlobalExceptionMiddleware>();
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // Explicitly add local.settings.json for local development
        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Create and configure AppSettings
        var appSettings = context.Configuration.GetAppSettings();
        
        // Register AppSettings using IOptions pattern with direct configuration binding
        services.Configure<AppSettings>(context.Configuration);
        
        // Also register as singleton for direct access
        services.AddSingleton(appSettings);

        // Add CORS policy for local development
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                policy.WithOrigins(
                     "http://localhost:3000",
                    "https://localhost:3000",
                    "http://127.0.0.1:3000",
                    "https://127.0.0.1:3000",
                    "https://localhost:53000",
                    "http://localhost:53000", // Add HTTP version as well
                    "https://e17989558671.ngrok-free.app",
                    "https://*.ngrok-free.app", // Allow any ngrok subdomain
                    "https://*.devtunnels.ms"   // Add support for dev tunnels as well

                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowedToAllowWildcardSubdomains(); // Enable wildcard subdomains;
            });
        });

        // Register ACS services for dependency injection - Simplified Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ITeamsNotificationService, TeamsNotificationService>();
        services.AddScoped<IQueueService, QueueService>(); // Queue service for SignalR messaging
        services.AddHttpClient(); // Register HttpClientFactory
        services.AddScoped<IAzureAIService, AzureAIService>();
        services.AddScoped<IGraphApiService, GraphApiService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAzureAIAgentService, AzureAIAgentService>();
        
        // Register Teams Meeting and Interoperability services
        services.AddScoped<ITeamsMeetingService, TeamsMeetingService>();
        services.AddScoped<ITeamsInteropService, TeamsInteropService>();

        // Register Graph Client Factory for Teams notifications
        services.AddSingleton<IGraphClientFactory, O365C.ACS.Integration.API.Helpers.GraphClientFactory>();

        // Register Microsoft Graph service
        services.AddScoped<GraphServiceClient>(serviceProvider =>
        {
            //var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var tenantId = appSettings.AzureAd.TenantId;
            var clientId = appSettings.AzureAd.ClientId;
            var clientSecret = appSettings.AzureAd.ClientSecret;

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Azure AD configuration is missing. Please check AzureAd:TenantId, AzureAd:ClientId, and AzureAd:ClientSecret settings.");
            }

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            return new GraphServiceClient(credential);
        });

        // Register repositories
        services.AddScoped<IChatRepository, ChatRepository>();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
    })
    .ConfigureLogging(logging =>
    {
        // Configure structured logging for better monitoring
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            // Remove default Application Insights rule to ensure all logs are sent
            var toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });
    })
    .Build();

host.Run();
