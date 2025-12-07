/**
 * Create a new chat thread in Azure Communication Services
 * 
 * This function creates a new chat thread for customer support
 * Following Azure Communication Services best practices:
 * - Use backend API for thread creation
 * - Implement proper error handling
 * - Return thread ID for subsequent operations
 * 
 * @param displayName - Display name for the thread creator
 * @returns Promise<string> - Thread ID of the created chat thread
 */
export const createThread = async (displayName: string): Promise<string> => {
  try {
    if (!displayName?.trim()) {
      throw new Error('Display name is required for thread creation');
    }

    const response = await fetch('/api/acs/thread/create', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        topic: `Support chat for ${displayName}`,
        displayName: displayName.trim(),
        // Add timestamp for uniqueness
        timestamp: new Date().toISOString(),
      }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!data.threadId) {
      throw new Error('Invalid thread creation response from server');
    }

    console.log('[ACS Thread] Successfully created chat thread:', data.threadId);
    return data.threadId;
    
  } catch (error) {
    console.error('[ACS Thread] Failed to create thread:', error);
    throw new Error(`Failed to create chat thread: ${error}`);
  }
};