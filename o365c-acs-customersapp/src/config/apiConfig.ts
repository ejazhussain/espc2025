/**
 * Azure Functions API Configuration
 * Centralized configuration for ACS service API client
 * 
 * Following Azure best practices for:
 * - Environment-specific configuration
 * - Secure endpoint management
 * - Performance optimization
 */

export interface ACSApiConfig {
  baseURL: string;
  signalRBaseURL: string;
  timeout: number;
  retries: number;
  retryDelay: number;
}

/**
 * Default API configuration values
 * Optimized for Azure Functions and ACS performance
 */
export const DEFAULT_API_CONFIG: ACSApiConfig = {
  baseURL: process.env.REACT_APP_API_BASE_URL || 'http://localhost:7181',
  signalRBaseURL: process.env.REACT_APP_SIGNALR_BASE_URL || 'https://functionapp-acs-signalr.azurewebsites.net',  // SignalR runs on port 7071
  timeout: 30000,  // 30 seconds - adequate for Azure Functions cold start
  retries: 3,      // Reasonable retry count for transient failures
  retryDelay: 1000 // 1 second base delay for exponential backoff
};

/**
 * Creates API configuration with environment overrides
 * Allows for flexible configuration across environments
 */
export function createApiConfig(overrides?: Partial<ACSApiConfig>): ACSApiConfig {
  return {
    ...DEFAULT_API_CONFIG,
    ...overrides
  };
}

/**
 * Validates API configuration values
 * Ensures configuration meets Azure service requirements
 */
export function validateApiConfig(config: ACSApiConfig): boolean {
  if (!config.baseURL || config.baseURL.trim() === '') {
    console.error('[API Config] Base URL is required');
    return false;
  }

  if (config.timeout < 5000 || config.timeout > 300000) {
    console.warn('[API Config] Timeout should be between 5-300 seconds');
  }

  if (config.retries < 1 || config.retries > 10) {
    console.warn('[API Config] Retries should be between 1-10');
  }

  return true;
}