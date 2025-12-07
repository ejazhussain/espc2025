// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createApiConfig } from '../config/apiConfig';
import { createAzureFunctionsClient } from '../config/axiosClient';

export interface AIMessage {
  id: string;
  role: 'customer' | 'ai';
  content: string;
  timestamp: Date;
  confidenceScore?: number;
  recommendEscalation?: boolean;
  escalationReason?: string;
  suggestedActions?: string[];
  isSystemMessage?: boolean; // Mark messages that are UI feedback only, not part of conversation history
}

export interface AgentQueryRequest {
  query: string;
}

export interface AgentResponse {
  response: string;
  success: boolean;
  errorMessage?: string;
  confidenceScore: number;
  timestamp: string;
  metadata: Record<string, any>;
}



export class AgentService {
  private client;

  constructor(baseURL?: string) {
    const config = createApiConfig(baseURL ? { baseURL } : undefined);
    this.client = createAzureFunctionsClient(config);
    console.log('[Agent Service] Initialized with endpoint:', config.baseURL);
  }

  async sendMessage(query: string): Promise<AgentResponse> {
    try {
      console.log('[Agent Service] Sending query:', query.substring(0, 50) + '...');
      
      const request: AgentQueryRequest = { query };
      const response = await this.client.post<AgentResponse>('/api/agent/query', request);
      
      console.log('[Agent Service] ✅ Received response:', {
        success: response.data.success,
        confidenceScore: response.data.confidenceScore,
        timestamp: response.data.timestamp
      });
      
      return response.data;
    } catch (error: any) {
      console.error('[Agent Service] ❌ Failed to send query:', error);
      throw new Error(`Failed to send query: ${error.response?.data?.error || error.message}`);
    }
  }
}