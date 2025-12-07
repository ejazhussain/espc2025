/**
 * Azure Communication Services Type Definitions
 * Centralized interfaces matching backend clean architecture models
 * 
 * These types ensure type safety and consistency between
 * frontend and Azure Functions backend APIs.
 */

import { ChatClient, ChatThreadClient } from '@azure/communication-chat';

/**
 * Enhanced token response interface matching backend clean architecture models
 */
export interface ACSUserTokenResponse {
  token: string;
  identity: string;
  expiresOn: string;
  user: {
    communicationUserId: string;
    displayName?: string;
    createdAt?: string;
    status?: string;
  };
}

/**
 * Thread creation response interface matching backend models
 */
export interface CreateThreadResponse {
  threadId: string;
  success: boolean;
  actualTopic?: string;
  assignedDepartment?: string;
  initialAgentId?: string;
  initialAgentName?: string;
  createdAt?: string;
  estimatedResponseTime?: string;
  errorMessage?: string;
  errorCode?: string;
}

/**
 * Agent assignment response interface matching backend models
 */
export interface AssignAgentResponse {
  success: boolean;
  agentDisplayName?: string;
  agentUserId?: string;
  threadId?: string;  
  teamsUserId?: string;
  assignedAt?: string;
  errorMessage?: string;
  errorCode?: string;
}

/**
 * Endpoint response interface matching backend models
 */
export interface EndpointResponse {
  endpointUrl: string;
  isValid?: boolean;
  region?: string;
  capabilities?: string[];
  validatedAt?: string;
  errorMessage?: string;
}

/**
 * Enhanced interface for Azure Communication Services chat operations
 * Includes both direct ACS operations and backend API integrations
 */
export interface IACSService {
  // Backend API operations
  getToken(): Promise<ACSUserTokenResponse>;
  getEndpointUrl(): Promise<string>;
  createThread(displayName: string, customerEmail?: string, questionSummary?: string): Promise<string>;
  joinThread(threadId: string, userId: string, displayName: string, role?: string): Promise<boolean>;
  assignAgentUser(threadId: string): Promise<AssignAgentResponse | undefined>;
  sendMessageViaAPI(userId: string, displayName: string, threadId: string, message: string): Promise<boolean>;
  sendConversationHistory(threadId: string, conversationHistory: any[]): Promise<void>;
  
  // Direct ACS client operations
  initializeChat(token: string, userId: string, endpoint: string): Promise<ChatClient>;
  joinThreadClient(chatClient: ChatClient, threadId: string): Promise<ChatThreadClient>;
  sendMessage(threadClient: ChatThreadClient, message: string): Promise<void>;
  setupMessageListener(threadClient: ChatThreadClient, onMessage: (message: any) => void): void;
  cleanup(): Promise<void>;
}