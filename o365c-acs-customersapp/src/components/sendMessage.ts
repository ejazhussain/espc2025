/**
 * Send a message to an ACS chat thread
 * 
 * This function sends a message to the specified chat thread
 * Following Azure Communication Services best practices:
 * - Use backend API for message sending
 * - Validate all required parameters
 * - Implement proper error handling
 * 
 * @param userId - ACS user identity sending the message
 * @param displayName - Display name of the sender
 * @param threadId - ID of the chat thread
 * @param message - Message content to send
 * @returns Promise<boolean> - Success status of send operation
 */
export const sendMessage = async (
  userId: string,
  displayName: string,
  threadId: string,
  message: string
): Promise<boolean> => {
  try {
    if (!userId?.trim() || !displayName?.trim() || !threadId?.trim() || !message?.trim()) {
      throw new Error('User ID, display name, thread ID, and message are required');
    }

    const response = await fetch('/api/acs/message/send', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        userId: userId.trim(),
        displayName: displayName.trim(),
        threadId: threadId.trim(),
        message: message.trim(),
        timestamp: new Date().toISOString(),
        messageType: 'text',
      }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!data.success) {
      throw new Error('Message send operation failed on server');
    }

    console.log('[ACS Message] Successfully sent message to thread:', threadId);
    return true;
    
  } catch (error) {
    console.error('[ACS Message] Failed to send message:', error);
    throw new Error(`Failed to send message: ${error}`);
  }
};