import { ChatClient, ChatThreadClient } from '@azure/communication-chat';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import { AxiosInstance } from 'axios';

// Configuration and client setup
import { createApiConfig } from '../config/apiConfig';
import { createAzureFunctionsClient } from '../config/axiosClient';

// Type definitions
import { IACSService, ACSUserTokenResponse, AssignAgentResponse, CreateThreadResponse, EndpointResponse } from '../types/acsTypes';

// Teams notification service
import { teamsNotificationService } from './TeamsNotificationService';

/**
 * Thread item status enumeration for agent work item management
 * Must match backend AgentWorkItemStatus enum
 */
export enum ThreadItemStatus {
  PENDING = 'pending',      // Maps to backend: Unassigned
  CLAIMED = 'claimed',      // Maps to backend: Claimed
  ACTIVE = 'active',        // Maps to backend: Active
  RESOLVED = 'resolved',    // Maps to backend: Resolved
  CANCELLED = 'cancelled',  // Maps to backend: Cancelled
  CLOSED = 'closed',
  ESCALATED = 'escalated'
}

/**
 * Agent work item interface with comprehensive metadata
 */
export interface AgentWorkItem {
  id: string;
  threadId: string;
  status: ThreadItemStatus;
  agentId?: string;
  agentName?: string;
  customerName?: string;
  priority: 'low' | 'normal' | 'high' | 'urgent';
  department: string;
  createdAt: string;
  updatedAt: string;
  lastMessageAt?: string;
  metadata?: Record<string, any>;
}

/**
 * String constants for Azure Communication Services
 * Centralized error messages and user-facing text
 */
export const ACS_STRINGS = {
  unableToStartChat: 'Unable to start chat. Please try again later.',
  connectionFailed: 'Connection to chat service failed.',
  invalidCredentials: 'Invalid authentication credentials.',
  threadCreationFailed: 'Failed to create chat thread.',
  agentAssignmentFailed: 'Failed to assign support agent.',
  messageSendFailed: 'Failed to send message.',
  networkError: 'Network connection failed. Please check your internet connection.',
  serverError: 'Server error occurred. Please try again later.',
  timeoutError: 'Request timed out. Please try again.',
  rateLimitError: 'Too many requests. Please wait a moment and try again.',
} as const;

/**
 * Comprehensive Azure Communication Services client
 * 
 * This class provides both backend API integration and direct ACS client operations
 * following Azure security best practices:
 * 
 * - Backend API integration for secure token and thread management
 * - Direct ACS client for real-time chat operations
 * - Axios with retry logic and proper error handling
 * - Resource cleanup and connection management
 * - Enterprise logging for monitoring and troubleshooting
 * 
 * Reference: https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/chat/get-started
 */
export class AzureCommunicationService implements IACSService {
  private chatClient: ChatClient | null = null;
  private threadClient: ChatThreadClient | null = null;
  private apiClient: AxiosInstance;

  constructor(baseURL?: string) {
    // Create configuration with optional base URL override
    const config = createApiConfig(baseURL ? { baseURL } : undefined);

    // Initialize pre-configured Axios client with retry logic
    this.apiClient = createAzureFunctionsClient(config);

    console.log('[ACS Service] Initialized with endpoint:', config.baseURL);
  }

  // Backend API Operations - Using pre-configured Axios client with retry logic

  async getToken(): Promise<ACSUserTokenResponse> {
    try {
      console.log('[ACS Service] Requesting user token from backend');

      const response = await this.apiClient.post('/api/token/createUserToken', {}, {
        params: { scope: 'chat' } // Default scope
      });

      const tokenData: ACSUserTokenResponse = response.data;

      if (!tokenData.token || !tokenData.identity) {
        throw new Error('Invalid token response from server');
      }

      console.log('[ACS Service] Successfully acquired user token');
      return tokenData;

    } catch (error) {
      console.error('[ACS Service] Failed to get token:', error);
      throw error instanceof Error ? error : new Error(ACS_STRINGS.invalidCredentials);
    }
  }

  async getEndpointUrl(): Promise<string> {
    try {
      console.log('[ACS Service] Requesting endpoint URL from backend');

      const response = await this.apiClient.get('/api/config/getEndpoint');
      const data: EndpointResponse = response.data;

      if (!data.endpointUrl) {
        throw new Error('Invalid endpoint response from server');
      }

      console.log('[ACS Service] Successfully retrieved endpoint URL');
      return data.endpointUrl;

    } catch (error) {
      console.error('[ACS Service] Failed to get endpoint URL:', error);

      // Fallback to environment variable if available
      const fallbackEndpoint = process.env.REACT_APP_ACS_ENDPOINT_URL;
      if (fallbackEndpoint) {
        console.warn('[ACS Service] Using fallback endpoint from environment');
        return fallbackEndpoint;
      }

      throw error instanceof Error ? error : new Error(ACS_STRINGS.connectionFailed);
    }
  }

  /**
   * Extracts a concise topic from the customer's question
   * @param questionSummary - The full question summary from the customer
   * @returns A short topic suitable for chat title (max 50 chars)
   */
  public extractTopicFromQuestion(questionSummary: string): string {
    if (!questionSummary?.trim()) {
      return 'General Support';
    }

    // Clean and normalize the question
    const cleaned = questionSummary.trim();
    
    // Common support topic patterns with their simplified versions
    const topicPatterns = [
      { pattern: /password.*reset|reset.*password|forgot.*password|can't.*login|cannot.*login/i, topic: 'Password Reset' },
      { pattern: /account.*locked|locked.*account|access.*denied/i, topic: 'Account Access' },
      { pattern: /billing|payment|invoice|subscription|charge/i, topic: 'Billing Issue' },
      { pattern: /technical.*issue|bug|error|not.*working|doesn't.*work/i, topic: 'Technical Issue' },
      { pattern: /installation|install|setup|configuration/i, topic: 'Installation Help' },
      { pattern: /feature.*request|enhancement|suggestion/i, topic: 'Feature Request' },
      { pattern: /refund|cancel|cancellation/i, topic: 'Cancellation' },
      { pattern: /update|upgrade|version/i, topic: 'Update Support' },
      { pattern: /training|tutorial|how.*to|help.*with/i, topic: 'Training Support' },
      { pattern: /integration|connect|api/i, topic: 'Integration Help' },
    ];

    // Check for pattern matches
    for (const { pattern, topic } of topicPatterns) {
      if (pattern.test(cleaned)) {
        return topic;
      }
    }

    // If no pattern matches, extract first meaningful words (up to 3-4 words, max 50 chars)
    const words = cleaned
      .replace(/[^\w\s]/g, ' ') // Remove punctuation
      .split(/\s+/)
      .filter(word => word.length > 2) // Filter out short words
      .slice(0, 4); // Take first 4 meaningful words
    
    const extracted = words.join(' ');
    
    // Capitalize first letter and ensure it's not too long
    const capitalized = extracted.charAt(0).toUpperCase() + extracted.slice(1).toLowerCase();
    return capitalized.length > 50 ? capitalized.substring(0, 47) + '...' : capitalized || 'General Support';
  }

  /**
   * Formats a timestamp for chat title display
   * @param date - Date to format
   * @returns Formatted string like "Today 10:49" or "Yesterday 15:30" or "Dec 15 09:15"
   */
  private formatChatTimestamp(date: Date = new Date()): string {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const chatDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    
    const timeString = date.toLocaleTimeString('en-US', { 
      hour: '2-digit', 
      minute: '2-digit', 
      hour12: false 
    });
    
    if (chatDate.getTime() === today.getTime()) {
      return `Today ${timeString}`;
    } else if (chatDate.getTime() === yesterday.getTime()) {
      return `Yesterday ${timeString}`;
    } else {
      const monthName = date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      return `${monthName} ${timeString}`;
    }
  }

  async createThread(displayName: string, customerEmail?: string, questionSummary?: string): Promise<string> {
    try {
      if (!displayName?.trim()) {
        throw new Error('Display name is required for thread creation');
      }

      console.log('[ACS Service] Creating chat thread for:', displayName);

      // Generate descriptive topic from customer's question
      const chatTopic = questionSummary ? 
        this.extractTopicFromQuestion(questionSummary) : 
        'General Support';
      
      const chatTimestamp = this.formatChatTimestamp(new Date());
      const fullTopic = `${displayName.trim()} - ${chatTopic} (${chatTimestamp})`;

      const requestData = {
        displayName: displayName.trim(),
        customerEmail: customerEmail?.trim() || '',
        topic: fullTopic,
        timestamp: new Date().toISOString(),
      };

      const response = await this.apiClient.post('/api/chat/thread/create', requestData);
      const data: CreateThreadResponse = response.data;

      if (!data.success || !data.threadId) {
        throw new Error(data.errorMessage || 'Thread creation failed on server');
      }

      console.log('[ACS Service] Successfully created chat thread:', data.threadId);
      return data.threadId;

    } catch (error) {
      console.error('[ACS Service] Failed to create thread:', error);
      throw error instanceof Error ? error : new Error(ACS_STRINGS.threadCreationFailed);
    }
  }

  async joinThread(threadId: string, userId: string, displayName: string, role: string = 'customer'): Promise<boolean> {
    try {
      if (!threadId?.trim() || !userId?.trim() || !displayName?.trim()) {
        throw new Error('Thread ID, user ID, and display name are required');
      }

      console.log('[ACS Service] Joining thread:', threadId, 'with role:', role);

      const requestData = {
        threadId: threadId.trim(),
        userId: userId.trim(),
        displayName: displayName.trim(),
        role: role.trim(),
      };

      const response = await this.apiClient.post('/api/chat/thread/join', requestData);
      const data = response.data;

      if (!data.success) {
        const errorMsg = data.errorMessage || 'Thread join operation failed on server';
        console.error('[ACS Service] Join thread failed:', errorMsg);
        throw new Error(errorMsg);
      }

      console.log('[ACS Service] Successfully joined thread:', threadId, 'as', role);
      return true;

    } catch (error) {
      console.error('[ACS Service] Failed to join thread:', error);
      throw error instanceof Error ? error : new Error('Failed to join chat thread');
    }
  }

  async assignAgentUser(threadId: string): Promise<AssignAgentResponse | undefined> {
    try {
      if (!threadId?.trim()) {
        throw new Error('Thread ID is required for agent assignment');
      }

      console.log('[ACS Service] Requesting agent assignment for thread:', threadId);

      const requestData = {
        threadId: threadId.trim()        
      };

      const response = await this.apiClient.post('/api/agent/assignAgentUser', requestData);
      const data: AssignAgentResponse = response.data;

      if (!data.success || !data.agentDisplayName) {
        console.warn('[ACS Service] No agent available for assignment:', data.errorMessage);
        return undefined;
      }

      console.log('[ACS Service] Successfully assigned agent:', data.agentDisplayName);
      return data;

    } catch (error) {
      console.error('[ACS Service] Failed to assign agent:', error);
      return undefined; // Non-critical failure, continue without agent
    }
  }

  async sendMessageViaAPI(
    userId: string,
    displayName: string,
    threadId: string,
    message: string
  ): Promise<boolean> {
    try {
      if (!userId?.trim() || !displayName?.trim() || !threadId?.trim() || !message?.trim()) {
        throw new Error('User ID, display name, thread ID, and message are required');
      }

      console.log('[ACS Service] Sending message via API to thread:', threadId);

      const requestData = {
        userId: userId.trim(),
        displayName: displayName.trim(),
        threadId: threadId.trim(),
        message: message.trim(),
        timestamp: new Date().toISOString(),
        messageType: 'text',
      };

      const response = await this.apiClient.post('/api/chat/message/send', requestData);
      const data = response.data;

      if (!data.success) {
        throw new Error(data.errorMessage || 'Message send operation failed on server');
      }

      console.log('[ACS Service] Successfully sent message to thread:', threadId);
      return true;

    } catch (error) {
      console.error('[ACS Service] Failed to send message:', error);
      throw error instanceof Error ? error : new Error(ACS_STRINGS.messageSendFailed);
    }
  }

  async initializeChat(token: string, userId: string, endpoint: string): Promise<ChatClient> {
    try {
      if (!token?.trim() || !userId?.trim() || !endpoint?.trim()) {
        throw new Error('Token, user ID, and endpoint are required for chat initialization');
      }

      const tokenCredential = new AzureCommunicationTokenCredential(token);

      this.chatClient = new ChatClient(endpoint, tokenCredential);

      await this.chatClient.startRealtimeNotifications();

      console.log('[ACS Service] Successfully initialized chat client');
      return this.chatClient;
    } catch (error) {
      console.error('[ACS Service] Failed to initialize ACS chat:', error);
      throw new Error(`Chat initialization failed: ${error}`);
    }
  }

  async joinThreadClient(chatClient: ChatClient, threadId: string): Promise<ChatThreadClient> {
    try {
      if (!threadId?.trim()) {
        throw new Error('Thread ID is required for thread client');
      }

      this.threadClient = chatClient.getChatThreadClient(threadId);
      console.log('[ACS Service] Successfully created thread client');
      return this.threadClient;
    } catch (error) {
      console.error('[ACS Service] Failed to join chat thread:', error);
      throw new Error(`Thread join failed: ${error}`);
    }
  }

  async sendMessage(threadClient: ChatThreadClient, message: string): Promise<void> {
    try {
      if (!message?.trim()) {
        throw new Error('Message content is required');
      }

      const sendMessageOptions = {
        content: message.trim(),
        type: 'text' as const,
      };

      await threadClient.sendMessage(sendMessageOptions);
      console.log('[ACS Service] Successfully sent message via client');
    } catch (error) {
      console.error('[ACS Service] Failed to send message:', error);
      throw new Error(`Message send failed: ${error}`);
    }
  }

  /**
   * Sends conversation history from AI chat to ACS chat
   * @param threadId - The ACS thread ID
   * @param conversationHistory - Array of AI chat messages
   */
  async sendConversationHistory(threadId: string, conversationHistory: any[], customerName?: string): Promise<void> {
    try {
      if (!threadId?.trim()) {
        throw new Error('Thread ID is required to send conversation history');
      }

      if (!conversationHistory || conversationHistory.length === 0) {
        console.log('[ACS Service] No conversation history to send');
        return;
      }

      console.log('[ACS Service] Sending conversation history to thread:', threadId, 'Messages:', conversationHistory.length);

      // Debug: Log original messages before formatting
      console.log('[ACS Service] Original conversation history:', JSON.stringify(conversationHistory, null, 2));

      // Format conversation history for ACS chat
      const formattedHistory = this.formatConversationHistory(conversationHistory, customerName);

      // Debug: Log formatted messages
      console.log('[ACS Service] Formatted conversation history:', JSON.stringify(formattedHistory, null, 2));

      const requestData = {
        threadId: threadId.trim(),
        conversationHistory: formattedHistory
      };

      const response = await this.apiClient.post('/api/chat/history/send', requestData);
      const data = response.data;

      if (!data.success) {
        throw new Error(data.errorMessage || 'Failed to send conversation history');
      }

      console.log('[ACS Service] Successfully sent conversation history');
    } catch (error) {
      console.error('[ACS Service] Failed to send conversation history:', error);
      throw error instanceof Error ? error : new Error('Failed to send conversation history');
    }
  }

  /**
   * Formats AI chat messages for ACS chat display
   * @param messages - Array of AI chat messages
   * @param customerName - Name of the customer (optional, defaults to 'Customer')
   * @returns Formatted conversation history
   */
  private formatConversationHistory(messages: any[], customerName?: string): any[] {
    return messages.map((msg, index) => {
      const timestamp = msg.timestamp ? new Date(msg.timestamp) : new Date();

      let displayName = '';
      let content = msg.content || '';

      switch (msg.role) {
        case 'customer':
          displayName = customerName || 'Customer';
          break;
        case 'ai':
          displayName = 'AI Assistant';
          // Don't modify content - send clean messages
          // ShareHistoryTime = MinValue allows everyone to see all messages naturally
          break;
        case 'system':
          displayName = 'System';
          // Don't modify content - send clean messages
          break;
        default:
          displayName = 'User';
      }

      return {
        content: content,
        displayName: displayName,
        timestamp: timestamp.toISOString(),
        senderDisplayName: displayName,
        type: 'text',
        messageType: 'text'
      };
    });
  }

  setupMessageListener(threadClient: ChatThreadClient, onMessage: (message: any) => void): void {
    if (this.chatClient) {
      this.chatClient.on('chatMessageReceived', (e) => {
        console.log('[ACS Service] Message received:', e.id);
        onMessage(e);
      });

      this.chatClient.on('chatMessageEdited', (e) => {
        console.log('[ACS Service] Message edited:', e.id);
        onMessage(e);
      });

      this.chatClient.on('chatMessageDeleted', (e) => {
        console.log('[ACS Service] Message deleted:', e.id);
        onMessage(e);
      });

      this.chatClient.on('typingIndicatorReceived', (e) => {
        console.log('[ACS Service] Typing indicator received');
        onMessage({ type: 'typingIndicator', ...e });
      });

      this.chatClient.on('readReceiptReceived', (e) => {
        console.log('[ACS Service] Read receipt received');
        onMessage({ type: 'readReceipt', ...e });
      });

      console.log('[ACS Service] Message listeners configured');
    } else {
      console.warn('[ACS Service] Cannot setup message listeners - chat client not initialized');
    }
  }

  async cleanup(): Promise<void> {
    try {
      if (this.chatClient) {
        await this.chatClient.stopRealtimeNotifications();
        console.log('[ACS Service] Stopped real-time notifications');
      }

      this.chatClient = null;
      this.threadClient = null;
      console.log('[ACS Service] Cleanup completed successfully');
    } catch (error) {
      console.error('[ACS Service] Cleanup failed:', error);
    }
  }

  // Agent Work Item Management Operations

  /**
   * Sends Teams notification to available agents about a new chat request
   * 
   * @param threadId - The chat thread identifier
   * @param displayName - The customer's display name
   * @param questionSummary - Summary of the customer's question/request
   * @returns Promise<void> - Logs success/failure but doesn't throw to avoid disrupting chat setup
   */
  async sendTeamsNotificationToAgents(    
    threadId: string,     
    teamUserId: string,
    displayName: string,
    questionSummary?: string,
    chatTopic?: string,
    initialMessage?: string
  ): Promise<void> {
    try {
      console.log('[ACS Service] Sending Teams notification for new chat request with customer context');
      
      // Create comprehensive notification request with customer context
      const chatRequestForNotification: CreateThreadResponse = {
        threadId,
        success: true,
        createdAt: new Date().toISOString(),
        actualTopic: `Support chat for ${displayName}`
      };
      
      // Enhanced notification with customer name and timestamp for better agent experience
      const notificationResponse = await teamsNotificationService.notifySpecificAgent(
        chatRequestForNotification,
        teamUserId,
        'NORMAL', // Priority can be enhanced with smart detection later
        displayName, // Pass customer name for personalized notifications
        new Date(), // Pass current timestamp for context
        questionSummary, // Pass customer's question for context
        chatTopic, // Pass extracted topic for better categorization
        initialMessage // Pass initial message for preview
      );
      
      if (notificationResponse.success) {
        console.log('[ACS Service] ✅ Teams notifications sent successfully to agent:', {
          agentUserId: teamUserId,
          customerName: displayName,
          threadId: threadId
        });
      } else {
        console.warn('[ACS Service] ⚠️ Teams notification failed:', notificationResponse.message);
      }
    } catch (notificationError) {
      console.error('[ACS Service] ❌ Teams notification error:', notificationError);
      // Don't fail the entire chat setup if notifications fail
    }
  }

  // Agent Work Item Management Operations

  /**
   * Updates an agent work item status with comprehensive error handling and retry logic
   * 
   * This method follows Azure best practices:
   * - Uses pre-configured Axios client with retry logic
   * - Comprehensive input validation and sanitization
   * - Structured logging for monitoring and troubleshooting
   * - Proper error handling with meaningful error messages
   * - Security through request validation and parameter encoding
   * 
   * @param threadId - The chat thread identifier
   * @param status - The new status for the work item
   * @returns Promise resolving to the updated work item
   * @throws Error with detailed information for monitoring and debugging
   */
  /**
   * Checks if an agent has accepted/joined the work item
   * 
   * @param threadId - The chat thread identifier
   * @returns Promise resolving to true if agent has accepted (status >= Claimed), false otherwise
   */
  async isAgentAccepted(threadId: string): Promise<boolean> {
    try {
      if (!threadId?.trim()) {
        console.warn('[ACS Service] Cannot check agent status - thread ID is empty');
        return false;
      }

      console.log('[ACS Service] Checking if agent has accepted thread:', threadId);

      const response = await this.apiClient.get(`/api/agent/getAgentWorkItems`);
      
      const workItems: AgentWorkItem[] = response.data;
      
      // Debug: Log all work items and their thread IDs
      console.log('[ACS Service] Total work items returned:', workItems.length);
      workItems.forEach((item, index) => {
        console.log(`[ACS Service] Work item ${index + 1}:`, {
          id: item.id,
          threadId: item.threadId,
          status: item.status,
          agentId: item.agentId
        });
      });
      
      // The API returns work items where the thread ID is stored in the 'id' field
      const workItem = workItems.find(item => item.id === threadId || item.threadId === threadId);

      if (!workItem) {
        console.log('[ACS Service] Work item not found for thread:', threadId);
        console.log('[ACS Service] Searched in', workItems.length, 'work items');
        return false;
      }

      // Agent is considered "accepted" if:
      // - Status is CLAIMED (1), ACTIVE (2), or RESOLVED (3)
      // Status 0 (Unassigned) means work item is unassigned and customer is waiting
      // Status 4 (Cancelled) means agent rejected/cancelled the request
      const acceptedStatuses = [1, 2, 3]; // Claimed, Active, Resolved
      const statusNum = workItem.status as any;
      const isAccepted = acceptedStatuses.includes(statusNum);
      const isCancelled = statusNum === 4;

      console.log('[ACS Service] Agent acceptance status:', {
        threadId,
        id: workItem.id,
        status: workItem.status,
        statusNum: statusNum,
        statusName: statusNum === 0 ? 'Unassigned' : statusNum === 1 ? 'Claimed' : statusNum === 2 ? 'Active' : statusNum === 3 ? 'Resolved' : statusNum === 4 ? 'Cancelled' : 'Unknown',
        agentId: workItem.agentId,
        agentName: workItem.agentName,
        isAccepted,
        isCancelled
      });

      return isAccepted;

    } catch (error) {
      console.error('[ACS Service] Failed to check agent acceptance:', error);
      // Return false on error rather than throwing - this is a non-critical check
      return false;
    }
  }

  /**
   * Check if the chat request was cancelled by an agent
   * Returns true if work item status is Cancelled (4)
   */
  async isChatCancelled(threadId: string): Promise<boolean> {
    try {
      const workItems = await this.apiClient.get(`/api/agent/getAgentWorkItems`);
      const workItem = workItems.data.find((item: any) => item.id === threadId);
      
      if (!workItem) {
        console.log('[ACS Service] Work item not found - may have been deleted');
        return false;
      }

      const statusNum = workItem.status as any;
      const isCancelled = statusNum === 4; // Cancelled status

      console.log('[ACS Service] Chat cancellation check:', {
        threadId,
        status: statusNum,
        isCancelled
      });

      return isCancelled;
    } catch (error) {
      console.error('[ACS Service] Failed to check chat cancellation:', error);
      return false;
    }
  }

  /**
   * Deletes a work item (used when customer ends chat)
   * 
   * @param threadId - The chat thread identifier
   * @returns Promise resolving to true if deleted successfully, false otherwise
   */
  async deleteWorkItem(threadId: string): Promise<boolean> {
    try {
      if (!threadId?.trim()) {
        console.warn('[ACS Service] Cannot delete work item - thread ID is empty');
        return false;
      }

      console.log('[ACS Service] Deleting work item for thread:', threadId);

      const response = await this.apiClient.delete(`/api/agent/deleteWorkItem/${threadId}`);
      
      console.log('[ACS Service] Successfully deleted work item:', response.data);
      return response.data.success || true;

    } catch (error: any) {
      if (error.response?.status === 404) {
        // Work item already deleted or doesn't exist
        console.warn('[ACS Service] Work item not found (may have been already deleted):', threadId);
        return false;
      }
      console.error('[ACS Service] Error deleting work item:', error);
      return false;
    }
  }

  async updateAgentWorkItem(threadId: string, status: ThreadItemStatus): Promise<AgentWorkItem> {
    try {
      // Input validation following Azure security best practices
      if (!threadId?.trim()) {
        throw new Error('Thread ID is required and cannot be empty');
      }

      if (!Object.values(ThreadItemStatus).includes(status)) {
        throw new Error(`Invalid status: ${status}. Must be one of: ${Object.values(ThreadItemStatus).join(', ')}`);
      }

      console.log('[ACS Service] Updating agent work item status:', { threadId, status });

      const requestData = {
        status,
        updatedAt: new Date().toISOString(),
        metadata: {
          updatedBy: 'customer-app',
          source: 'chat-client',
          timestamp: new Date().toISOString()
        }
      };

      // Use URL encoding to prevent injection attacks
      const encodedThreadId = encodeURIComponent(threadId.trim());
      const response = await this.apiClient.put(`/api/agent/updateAgentWorkItems/${encodedThreadId}`, requestData);

      const workItem: AgentWorkItem = response.data;

      if (!workItem.id || !workItem.threadId) {
        throw new Error('Invalid work item response from server');
      }

      console.log('[ACS Service] Successfully updated agent work item:', {
        threadId: workItem.threadId,
        status: workItem.status,
        updatedAt: workItem.updatedAt
      });

      return workItem;

    } catch (error) {
      console.error('[ACS Service] Failed to update agent work item:', {
        threadId,
        status,
        error: error instanceof Error ? error.message : String(error)
      });

      // Re-throw with enhanced error information for debugging
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      throw new Error(`Failed to update agent work item for thread ${threadId}: ${errorMessage}`);
    }
  }

}

export default AzureCommunicationService;