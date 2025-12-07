import axios, { AxiosInstance, AxiosError, AxiosRequestConfig } from 'axios';
import { ACSApiConfig } from './apiConfig';
import { ACS_STRINGS } from '../services/AzureCommunicationService';

/**
 * Azure Functions Axios Client Factory
 * Creates pre-configured Axios instances following Azure best practices
 * 
 * Features:
 * - Automatic retry logic with exponential backoff
 * - Enterprise logging and monitoring
 * - Error handling optimized for Azure services
 * - Request/response interceptors for debugging
 */

/**
 * Creates configured Axios client for Azure Functions API
 * Implements enterprise-grade retry and error handling
 */
export function createAzureFunctionsClient(config: ACSApiConfig): AxiosInstance {
  const client = axios.create({
    baseURL: config.baseURL,
    timeout: config.timeout,
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    }
  });

  // Request interceptor for logging
  client.interceptors.request.use(
    (requestConfig) => {
      console.log(`[ACS API] ${requestConfig.method?.toUpperCase()} ${requestConfig.url}`);
      return requestConfig;
    },
    (error) => {
      console.error('[ACS API] Request error:', error);
      return Promise.reject(error);
    }
  );

  // Response interceptor with retry logic
  client.interceptors.response.use(
    (response) => {
      console.log(`[ACS API] Success: ${response.status} ${response.config.url}`);
      return response;
    },
    async (error: AxiosError) => {
      const requestConfig = error.config as AxiosRequestConfig & { _retry?: number };
      
      // Log error details for debugging
      console.error(`[ACS API] Error: ${error.response?.status || 'Network'} ${requestConfig?.url}`, {
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
      });

      // Retry logic for transient failures
      if (shouldRetryRequest(error) && (!requestConfig._retry || requestConfig._retry < config.retries)) {
        requestConfig._retry = (requestConfig._retry || 0) + 1;
        
        const delay = calculateRetryDelay(requestConfig._retry, config.retryDelay);
        console.log(`[ACS API] Retrying in ${delay}ms (attempt ${requestConfig._retry}/${config.retries})`);
        
        await new Promise(resolve => setTimeout(resolve, delay));
        return client.request(requestConfig);
      }

      return Promise.reject(createUserFriendlyError(error));
    }
  );

  return client;
}

/**
 * Determines if a request should be retried
 * Based on Azure service error patterns
 */
function shouldRetryRequest(error: AxiosError): boolean {
  // Retry on network errors (no response)
  if (!error.response) return true;

  const status = error.response.status;
  
  // Retry on server errors and rate limiting
  return status >= 500 || status === 429 || status === 408;
}

/**
 * Calculates exponential backoff delay with jitter
 * Prevents thundering herd problems in distributed systems
 */
function calculateRetryDelay(retryCount: number, baseDelay: number): number {
  const exponentialDelay = baseDelay * Math.pow(2, retryCount - 1);
  const jitter = Math.random() * 1000; // Add randomness
  return Math.min(exponentialDelay + jitter, 30000); // Cap at 30 seconds
}

/**
 * Creates user-friendly error messages from Axios errors
 * Maps HTTP status codes to meaningful user messages
 */
function createUserFriendlyError(error: AxiosError): Error {
  if (!error.response) {
    return new Error(ACS_STRINGS.networkError);
  }

  const status = error.response.status;
  const errorData = error.response.data as any;

  switch (status) {
    case 400:
      return new Error(errorData?.error || 'Invalid request parameters');
    case 401:
      return new Error(ACS_STRINGS.invalidCredentials);
    case 403:
      return new Error('Access forbidden. Please check your permissions.');
    case 404:
      return new Error('Service endpoint not found. Please check configuration.');
    case 408:
      return new Error(ACS_STRINGS.timeoutError);
    case 429:
      return new Error(ACS_STRINGS.rateLimitError);
    case 500:
      return new Error(ACS_STRINGS.serverError);
    default:
      return new Error(`Service error (${status}): ${errorData?.error || error.message}`);
  }
}