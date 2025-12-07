/**
 * Assign a support agent to a chat thread
 * 
 * This function assigns an available support agent to the chat thread
 * Following enterprise support best practices:
 * - Use backend API for agent assignment logic
 * - Implement load balancing for agent distribution
 * - Return agent information for UI display
 * 
 * @param threadId - ID of the chat thread
 * @returns Promise<string | undefined> - Display name of assigned agent
 */
export const assignAgentUser = async (threadId: string): Promise<string | undefined> => {
  try {
    if (!threadId?.trim()) {
      throw new Error('Thread ID is required for agent assignment');
    }

    const response = await fetch('/api/agent/assign', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        threadId: threadId.trim()       
      }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!data.success || !data.agentDisplayName) {
      console.warn('[ACS Agent] No agent available for assignment');
      return undefined;
    }

    console.log('[ACS Agent] Successfully assigned agent:', data.agentDisplayName);
    return data.agentDisplayName;
    
  } catch (error) {
    console.error('[ACS Agent] Failed to assign agent:', error);
    return undefined;
  }
};