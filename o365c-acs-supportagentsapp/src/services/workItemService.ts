// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';
import { ThreadItemStatus } from '../components/chat/useThreads';

export interface AgentWorkItem {
  id: string;
  status: ThreadItemStatus;
  assignedAgentId?: string | null;
  assignedAgentName?: string | null;
  claimedAt?: string | null;
  customerName?: string;
  createdAt: string;
  updatedAt: string;
  waitTimeSeconds?: number;
  priority?: string;
}

export interface ClaimWorkItemRequest {
  agentId: string;
  agentName: string;
}

export interface ClaimWorkItemResult {
  success: boolean;
  workItem?: AgentWorkItem;
  error?: string;
  claimedBy?: string;
  claimedAt?: string;
}

export const getAgentWorkItems = async (): Promise<AgentWorkItem[]> => {
  try {
    const response = await apiService.get(`/agent/getAgentWorkItems`);
    return response.data;
  } catch (error) {
    throw new Error('Failed at getting agent work items, Error: ' + error);
  }
};

/**
 * Gets cancelled work items (for tracking cancelled chats)
 */
export const getCancelledWorkItems = async (): Promise<AgentWorkItem[]> => {
  try {
    const response = await apiService.get(`/agent/getAgentWorkItems?status=4`); // status=4 is Cancelled
    return response.data;
  } catch (error) {
    throw new Error('Failed at getting cancelled work items, Error: ' + error);
  }
};

export const createAgentWorkItem = async (threadId: string, status: ThreadItemStatus): Promise<AgentWorkItem> => {
  try {
    const response = await apiService.post('/agent/createAgentWorkItems', { id: threadId, status: status });
    return response.data;
  } catch (error) {
    throw new Error(`Failed at creating agent work item for threadId ${threadId}, Error: ` + error);
  }
};

export const updateAgentWorkItem = async (threadId: string, status: ThreadItemStatus): Promise<AgentWorkItem> => {
  try {
    const response = await apiService.put(`/agent/updateAgentWorkItems/${threadId}`, { status: status });
    return response.data;
  } catch (error) {
    throw new Error(`Failed at updating agent work item for threadId ${threadId}, Error: ` + error);
  }
};

export const deleteAgentWorkItem = async (threadId: string): Promise<void> => {
  try {
    await apiService.delete(`/chat/thread/${threadId}`);
  } catch (error) {
    throw new Error(`Failed at deleting agent work item for threadId ${threadId}, Error: ` + error);
  }
};

/**
 * Cancels/Deletes a work item (used when agent cancels or customer ends chat)
 */
export const cancelWorkItem = async (threadId: string): Promise<boolean> => {
  try {
    const response = await apiService.delete(`/agent/deleteWorkItem/${threadId}`);
    return response.data.success;
  } catch (error: any) {
    if (error.response?.status === 404) {
      // Work item already deleted or doesn't exist
      return false;
    }
    throw new Error(`Failed to cancel work item ${threadId}: ${error.message}`);
  }
};

/**
 * Atomically claims a work item for an agent
 * Returns success if claimed, or error if already claimed by another agent
 */
export const claimWorkItem = async (
  threadId: string,
  agentId: string,
  agentName: string
): Promise<ClaimWorkItemResult> => {
  try {
    const response = await apiService.post(`/agent/claimWorkItem/${threadId}`, {
      agentId,
      agentName,
    });
    return response.data;
  } catch (error: any) {
    // Handle 409 Conflict (race condition - already claimed)
    if (error.response?.status === 409) {
      return error.response.data;
    }
    throw new Error(`Failed to claim work item ${threadId}: ${error.message}`);
  }
};

/**
 * Gets all unassigned work items (in the queue)
 * Ordered by wait time (oldest first)
 */
export const getUnassignedWorkItems = async (): Promise<AgentWorkItem[]> => {
  try {
    const response = await apiService.get('/agent/getUnassignedWorkItems');
    return response.data.items || [];
  } catch (error) {
    throw new Error('Failed to get unassigned work items: ' + error);
  }
};

/**
 * Gets all work items assigned to a specific agent
 * Optional status filter: 'claimed', 'active', or 'resolved'
 */
export const getMyWorkItems = async (
  agentId: string,
  status?: 'claimed' | 'active' | 'resolved'
): Promise<AgentWorkItem[]> => {
  try {
    const params = status ? { status } : {};
    const response = await apiService.get(`/agent/getMyWorkItems/${agentId}`, { params });
    return response.data.items || [];
  } catch (error) {
    throw new Error(`Failed to get work items for agent ${agentId}: ${error}`);
  }
};
