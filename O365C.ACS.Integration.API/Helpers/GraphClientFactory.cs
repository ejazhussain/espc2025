using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using O365C.ACS.Integration.API.Models.Settings;

namespace O365C.ACS.Integration.API.Helpers
{
    public interface IGraphClientFactory
    {
        GraphServiceClient CreateTeamsGraphClient();
    }

    public class GraphClientFactory : IGraphClientFactory
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<GraphClientFactory> _logger;
        private GraphServiceClient? _teamsGraphClient;
        private readonly object _lockObject = new object();

        public GraphClientFactory(IOptions<AppSettings> appSettings, ILogger<GraphClientFactory> logger)
        {
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public GraphServiceClient CreateTeamsGraphClient()
        {
            if (_teamsGraphClient != null)
                return _teamsGraphClient;

            lock (_lockObject)
            {
                if (_teamsGraphClient != null)
                    return _teamsGraphClient;

                try
                {
                    // Read Teams app configuration from AppSettings
                    var clientId = _appSettings.TeamsApp.ClientId;
                    var clientSecret = _appSettings.TeamsApp.ClientSecret;
                    var tenantId = _appSettings.TeamsApp.TenantId;

                    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
                    {
                        throw new InvalidOperationException("TeamsApp credentials (ClientId, ClientSecret, TenantId) are required for Teams operations");
                    }

                    _logger.LogInformation("Creating Graph client with Teams app credentials - ClientId: {ClientId}", clientId);

                    // Use client credentials flow with .default scope
                    var scopes = new[] { "https://graph.microsoft.com/.default" };

                    var options = new ClientSecretCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    };

                    var clientSecretCredential = new ClientSecretCredential(
                        tenantId, clientId, clientSecret, options);

                    _teamsGraphClient = new GraphServiceClient(clientSecretCredential, scopes);

                    _logger.LogInformation("Graph client created successfully for Teams operations");
                    return _teamsGraphClient;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create Teams Graph client");
                    throw;
                }
            }
        }
    }
}