import { AxiosInstance } from 'axios';
import { CreateThreadResponse } from '../types/acsTypes';

// Configuration and client setup
import { createApiConfig } from '../config/apiConfig';
import { createAzureFunctionsClient } from '../config/axiosClient';

export interface TeamsNotificationRequest {
  chatRequest: CreateThreadResponse;
  agentUserId?: string;
  priority?: 'NORMAL' | 'HIGH' | 'CRITICAL';
  customerName?: string;
  requestTime?: string; // ISO string format
  questionSummary?: string;
  chatTopic?: string;
  initialMessage?: string;
}

export interface TeamsNotificationResponse {
  success: boolean;
  message: string;
  threadId?: string;
  timestamp?: string;
}

class TeamsNotificationService {
  private apiClient: AxiosInstance;

  constructor(baseURL?: string) {
    // Create configuration with optional base URL override
    const config = createApiConfig(baseURL ? { baseURL } : undefined);

    // Initialize pre-configured Axios client with retry logic
    this.apiClient = createAzureFunctionsClient(config);

    console.log('[Teams Notification Service] Initialized with endpoint:', config.baseURL);
  }

  /**
   * Send Teams activity notification to a specific agent
   */
  async notifySpecificAgent(    
    chatRequest: CreateThreadResponse, 
    agentUserId: string,
    priority: 'NORMAL' | 'HIGH' | 'CRITICAL' = 'NORMAL',
    customerName?: string,
    requestTime?: Date,
    questionSummary?: string,
    chatTopic?: string,
    initialMessage?: string
  ): Promise<TeamsNotificationResponse> {
    try {
      console.log('[Teams Notification Service] Sending notification to specific agent:', {
        agentUserId,
        threadId: chatRequest.threadId,
        priority,
        customerName,
        requestTime: requestTime?.toISOString(),
        questionSummary,
        chatTopic,
        initialMessage: initialMessage?.substring(0, 50) + '...' // Log truncated for privacy
      });

      const notificationRequest: TeamsNotificationRequest = {
        chatRequest,
        agentUserId,
        priority,
        customerName,
        requestTime: requestTime?.toISOString(),
        questionSummary,
        chatTopic,
        initialMessage
      };

      const response = await this.apiClient.post('/api/teams/notify', notificationRequest);
      const result: TeamsNotificationResponse = response.data;
      
      console.log('[Teams Notification Service] ✅ Teams notification sent to specific agent:', {
        agentUserId,
        threadId: chatRequest.threadId,
        priority,
        customerName,
        success: result.success
      });

      return result;
    } catch (error) {
      console.error('[Teams Notification Service] ❌ Failed to send Teams notification to specific agent:', error);
      return {
        success: false,
        message: `Failed to send notification: ${error instanceof Error ? error.message : 'Unknown error'}`,
        threadId: chatRequest.threadId,
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Send notification with automatic priority detection
   * This uses the assigned agent instead of broadcasting to all agents
   */
  async sendSmartNotification(
    chatRequest: CreateThreadResponse, 
    agentUserId: string,
    customerMessage?: string,
    customerName?: string,
    requestTime?: Date,
    questionSummary?: string,
    chatTopic?: string
  ): Promise<TeamsNotificationResponse> {
    // Simple priority detection based on keywords
    const priority = this.detectPriority(customerMessage);
    
    console.log('[Teams Notification Service] 🤖 Smart notification with detected priority:', {
      threadId: chatRequest.threadId,
      agentUserId,
      priority,
      customerName,
      customerMessage: customerMessage?.substring(0, 100) + '...'
    });

    return this.notifySpecificAgent(
      chatRequest, 
      agentUserId, 
      priority, 
      customerName, 
      requestTime,
      questionSummary,
      chatTopic,
      customerMessage
    );
  }

  /**
   * Detect priority level based on customer message content
   */
  private detectPriority(message?: string): 'NORMAL' | 'HIGH' | 'CRITICAL' {
    if (!message) return 'NORMAL';

    const criticalKeywords = ['urgent', 'emergency', 'critical', 'down', 'broken', 'not working', 'help'];
    const highKeywords = ['important', 'asap', 'quickly', 'soon', 'problem', 'issue'];

    const lowerMessage = message.toLowerCase();

    if (criticalKeywords.some(keyword => lowerMessage.includes(keyword))) {
      return 'CRITICAL';
    }

    if (highKeywords.some(keyword => lowerMessage.includes(keyword))) {
      return 'HIGH';
    }

    return 'NORMAL';
  }
}

// Export singleton instance
export const teamsNotificationService = new TeamsNotificationService();

// Export class for dependency injection if needed
export default TeamsNotificationService;
