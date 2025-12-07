/**
 * Azure AD B2C configuration interface for MSAL authentication
 * Follows Azure best practices for secure configuration management
 */
export interface AuthConfig {
  clientId: string;
  authority: string;
  redirectUri: string;
  postLogoutRedirectUri?: string;
  scopes: string[];
}

/**
 * Application configuration interface
 */
export interface AppConfig {
  auth: AuthConfig;
  acs: {
    endpointUrl: string;
    scope: string;
  };
  environment: 'development' | 'staging' | 'production';
}

/**
 * User profile information from Microsoft Graph
 */
export interface UserProfile {
  id: string;
  displayName: string;
  givenName?: string;
  surname?: string;
  userPrincipalName: string;
  mail?: string;
  jobTitle?: string;
  department?: string;
  officeLocation?: string;
  businessPhones?: string[];
  mobilePhone?: string;
}