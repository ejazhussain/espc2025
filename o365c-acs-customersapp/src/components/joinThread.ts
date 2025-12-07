/**
 * Join a user to an existing chat thread
 * 
 * This function adds a user to an ACS chat thread
 * Following Azure Communication Services best practices:
 * - Use backend API for secure thread joining
 * - Validate user identity and thread ID
 * - Implement proper error handling
 * - Support role-based message history visibility
 * 
 * @param threadId - ID of the chat thread to join
 * @param userId - ACS user identity
 * @param displayName - Display name for the user
 * @param role - User role: 'customer' (default), 'agent', 'supervisor', 'member'
 * @returns Promise<boolean> - Success status of join operation
 */
export const joinThread = async (
  threadId: string, 
  userId: string, 
  displayName: string,
  role: string = 'customer'
): Promise<boolean> => {
  try {
    if (!threadId?.trim() || !userId?.trim() || !displayName?.trim()) {
      throw new Error('Thread ID, user ID, and display name are required');
    }

    const response = await fetch('/api/acs/thread/join', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        threadId: threadId.trim(),
        userId: userId.trim(),
        displayName: displayName.trim(),
        role: role.trim(),
      }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!data.success) {
      throw new Error('Thread join operation failed on server');
    }

    console.log('[ACS Join] Successfully joined thread:', threadId);
    return true;
    
  } catch (error) {
    console.error('[ACS Join] Failed to join thread:', error);
    return false;
  }
};