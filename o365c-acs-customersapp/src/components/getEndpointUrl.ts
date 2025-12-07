/**
 * Get Azure Communication Services endpoint URL
 * 
 * This function retrieves the ACS endpoint URL from your backend
 * Following Azure security best practices:
 * - Endpoint URL can be public but should be centrally managed
 * - Use environment variables or configuration service
 * 
 * @returns Promise<string> - ACS endpoint URL
 */
export const getEndpointUrl = async (): Promise<string> => {
  try {
    // Option 1: Get from backend API (recommended)
    const response = await fetch('/api/acs/endpoint', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!data.endpointUrl) {
      throw new Error('Invalid endpoint response from server');
    }

    console.log('[ACS Endpoint] Successfully retrieved endpoint URL');
    return data.endpointUrl;
    
  } catch (error) {
    console.error('[ACS Endpoint] Failed to get endpoint URL:', error);
    
    // Option 2: Fallback to environment variable (less secure)
    const fallbackEndpoint = process.env.REACT_APP_ACS_ENDPOINT_URL;
    if (fallbackEndpoint) {
      console.warn('[ACS Endpoint] Using fallback endpoint from environment');
      return fallbackEndpoint;
    }
    
    throw new Error(`Failed to acquire ACS endpoint URL: ${error}`);
  }
};