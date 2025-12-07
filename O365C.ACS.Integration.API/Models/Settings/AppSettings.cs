using System.ComponentModel.DataAnnotations;
using O365C.ACS.Integration.API.Models.Agent;

namespace O365C.ACS.Integration.API.Models.Settings
{
    public class AppSettings
    {
        public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
        public CosmosDbSettings CosmosDb { get; set; } = new CosmosDbSettings();
        public ACSSettings ACS { get; set; } = new ACSSettings();
        public AgentUsersSettings AgentUsers { get; set; } = new AgentUsersSettings();
        public TeamsAppSettings TeamsApp { get; set; } = new TeamsAppSettings();
        public AzureAdSettings AzureAd { get; set; } = new AzureAdSettings();
        public AzureOpenAISettings AzureOpenAI { get; set; } = new AzureOpenAISettings();
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();        
        public AzureAIAgentSettings AzureAIAgent { get; set; } = new AzureAIAgentSettings();
    }

    public class ConnectionStringsSettings
    {
        public string AzureCommunicationServices { get; set; } = string.Empty;
        public string CosmosDb { get; set; } = string.Empty;
    }

    public class CosmosDbSettings
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty;
    }

    public class ACSSettings
    {
        [Required]
        public string EndpointUrl { get; set; } = string.Empty;

        [Required]
        public string AdminUserId { get; set; } = string.Empty;

        public string DefaultTokenScope { get; set; } = "chat";

        public int TokenExpirationHours { get; set; } = 24;

        public int MaxConcurrentOperations { get; set; } = 10;
    }

    public class AgentUsersSettings
    {
        public List<AgentUser> Users { get; set; } = new List<AgentUser>()
        {
            new AgentUser
            {
                TeamsUserId = "2a5de346-1d63-4c7a-897f-b1f4b5316fe5",
                AcsUserId = "8:acs:9dbd9aec-1230-4ddb-89c3-4e9a3540be29_00000029-d06f-6307-28d2-493a0d00b5f7",
                DisplayName = "Ejaz Hussain"
            },
            new AgentUser
            {
                TeamsUserId = "af27a856-3780-45ff-a888-09984b980e8e",
                AcsUserId = "8:acs:9dbd9aec-1230-4ddb-89c3-4e9a3540be29_00000029-afdb-a8ff-49a1-473a0d0002a6",
                DisplayName = "Bella Barlow"
            }
        };
    }

    public class TeamsAppSettings
    {
        [Required]
        public string AppId { get; set; } = string.Empty;

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string TeamId { get; set; } = string.Empty;

        public bool EnableNotifications { get; set; } = true;

        // Frontend Azure AD app credentials for Teams operations
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        [Required]
        public string TenantId { get; set; } = string.Empty;
    }

    public class AzureAdSettings
    {
        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class AzureOpenAISettings
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string DeploymentName { get; set; } = string.Empty;

        public string ApiVersion { get; set; } = "2024-05-01-preview";
    }

    public class EmailSettings
    {
        /// <summary>
        /// The service account email address used to send emails via Microsoft Graph
        /// This account must have Mail.Send permissions
        /// </summary>
        public string ServiceAccountEmail { get; set; } = "support@office365clinic.com";
    }

    public class AzureAISearchSettings
    {
        /// <summary>
        /// Azure AI Search service name (without .search.windows.net)
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Azure AI Search API key for authentication
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Name of the search index for knowledge base queries
        /// </summary>
        public string IndexName { get; set; } = "support-knowledge-base";
    }

    public class AzureAIAgentSettings
    {
        /// <summary>
        /// Azure AI Project connection string (endpoint URL)
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Azure AI Agent ID
        /// </summary>
        [Required]
        public string AgentId { get; set; } = string.Empty;
    }
}