// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import AzureCommunicationService, { ACS_STRINGS } from '../services/AzureCommunicationService';

/**
 * Props interface for creating and joining chat thread with new user
 * Follows Azure Communication Services patterns for chat initialization
 */
export interface CreateAndJoinChatThreadWithNewUserProps {
  displayName: string;
  customerEmail: string;
  questionSummary: string;
  conversationHistory?: any[]; // Optional conversation history from AI chat
  onJoinChat(): void;
  setToken(token: string): void;
  setUserId(userId: string): void;
  setThreadId(threadId: string): void;
  setEndpointUrl(endpointUrl: string): void;
  setAgentName(agentName: string): void;
  onError(error: string, questionSummary: string): void;
}

/**
 * Creates a new chat thread and ACS user, then joins the thread
 * 
 * This function implements Azure Communication Services best practices:
 * - Uses centralized ACS service for all operations
 * - Proper error handling for each ACS operation
 * - Sequential execution to ensure dependencies are met
 * - Agent assignment for enterprise support scenarios
 * - Initial message sending for context
 * 
 * Security considerations:
 * - Uses secure token acquisition via backend API
 * - Validates all operations before proceeding
 * - Implements proper error propagation
 * 
 * @param props - Configuration and callback functions for chat initialization
 */
export const createAndJoinChatThreadWithNewUser = (props: CreateAndJoinChatThreadWithNewUserProps): void => {
  const {
    displayName,
    customerEmail,
    questionSummary,
    conversationHistory, // Kept for future use, currently not sent (ShareHistoryTime = MinValue shows all messages)
    onJoinChat,
    setToken,
    setUserId,
    setThreadId,
    setEndpointUrl,
    setAgentName,
    onError
  } = props;

  /**
   * Main chat thread creation and joining workflow
   * Uses consolidated AzureCommunicationService for all operations
   */
  const createAndJoinChatThread = async (): Promise<void> => {
    const acsService = new AzureCommunicationService();

    try {
      console.log('[Chat Setup] Starting chat initialization workflow');

      // Step 1: Create a new chat thread
      // This establishes the communication channel for the support session
      const threadId = await acsService.createThread(displayName, customerEmail, questionSummary);
      if (!threadId) {
        console.error('[Chat Setup] Failed to create thread, returned threadId is undefined or empty');
        onError(ACS_STRINGS.unableToStartChat, questionSummary);
        return;
      }
      setThreadId(threadId);
      console.log('[Chat Setup] Successfully created chat thread:', threadId);

      // Step 1.5: Send conversation history BEFORE customer joins (from AI chat escalation)
      // Since ShareHistoryTime is set to MinValue, everyone will see these messages
      // This provides full context of the AI conversation before escalation to human agent
      if (conversationHistory && conversationHistory.length > 0) {
        try {
          console.log('[Chat Setup] Sending conversation history with', conversationHistory.length, 'messages');
          await acsService.sendConversationHistory(threadId, conversationHistory, displayName);
          console.log('[Chat Setup] Successfully sent conversation history');
        } catch (historyError) {
          console.warn('[Chat Setup] Failed to send conversation history, but continuing with chat setup:', historyError);
          // Continue with chat setup even if history sending fails
        }
      }

      // Step 2: Create a new ACS user and get authentication token
      // This provides secure access credentials for the communication session
      const token = await acsService.getToken();
      const endpointUrl = await acsService.getEndpointUrl();

      if (!token?.token || !token?.identity) {
        console.error('[Chat Setup] Failed to acquire user token or identity');
        onError(ACS_STRINGS.unableToStartChat, questionSummary);
        return;
      }

      setToken(token.token);
      setUserId(token.identity);
      setEndpointUrl(endpointUrl);
      console.log('[Chat Setup] Successfully acquired user credentials', {
        userId: token.identity,
        endpointUrl: endpointUrl,
        tokenLength: token.token?.length || 0,
        tokenPrefix: token.token?.substring(0, 20) + '...',
        threadId: threadId
      });

      // Step 3: Join the thread with the newly created user as a customer
      // This establishes the user's participation in the chat thread
      // Customers only see messages from when they join (no AI conversation history)
      try {
        const result = await acsService.joinThread(threadId, token.identity, displayName, 'customer');
        if (!result) {
          console.error('[Chat Setup] Failed to join the thread:', threadId);
          onError(ACS_STRINGS.unableToStartChat, questionSummary);
          return;
        }
        console.log('[Chat Setup] Successfully joined chat thread as customer', {
          threadId: threadId,
          userId: token.identity,
          displayName: displayName,
          role: 'customer',
          joinResult: result
        });
      } catch (joinError) {
        console.error('[Chat Setup] Error joining thread:', joinError);
        onError(`Failed to join chat thread: ${joinError instanceof Error ? joinError.message : 'Unknown error'}`, questionSummary);
        return;
      }

      // Step 4: Chat is now created and appears in agent queue
      // Agent will be assigned when they accept the chat from their queue
      // No premature agent assignment here
      console.log('[Chat Setup] Chat thread created and added to agent queue', {
        threadId: threadId,
        currentUserId: token.identity
      });
      setAgentName('Support Agent'); // Generic name until agent accepts

      // Step 6: Send the initial message with the question summary
      // Only send if we haven't already sent conversation history
      // (conversation history already includes the customer's messages)
      if (questionSummary?.trim() && (!conversationHistory || conversationHistory.length === 0)) {
        try {
          await acsService.sendMessageViaAPI(token.identity, displayName, threadId, questionSummary);
          console.log('[Chat Setup] Successfully sent initial message');
        } catch (messageError) {
          console.warn('[Chat Setup] Failed to send initial message, but continuing with chat setup:', messageError);
          // Continue with chat setup even if initial message fails
        }
      } else if (conversationHistory && conversationHistory.length > 0) {
        console.log('[Chat Setup] Skipping initial message - conversation history already sent');
      }

      // Step 5: Complete the chat initialization
      // Notify the parent component that chat is ready
      onJoinChat();
      console.log('[Chat Setup] Chat initialization completed successfully', {
        threadId: threadId,
        userId: token.identity,
        status: 'Waiting for agent to accept',
        timestamp: new Date().toISOString()
      });

      // Note: Teams notification will be sent when an agent accepts the chat from their queue
      // This is handled by the agent service in the backend


    } catch (error) {
      // Global error handler for any unexpected failures
      console.error('[Chat Setup] An unexpected error occurred during chat initialization:', error);
      onError(ACS_STRINGS.unableToStartChat, questionSummary);
    } finally {
      // Cleanup any resources if needed
      // Note: We don't cleanup here as the chat client will be used by the UI
    }
  };

  // Execute the chat thread creation workflow
  // This is done immediately when the function is called
  createAndJoinChatThread();
};