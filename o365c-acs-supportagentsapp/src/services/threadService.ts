// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';
import { threadStrings } from '../constants/constants';

export interface ThreadDeleteResult {
  success: boolean;
  threadId: string;
  message?: string;
}

/**
 * Service for managing chat threads
 * Handles all thread-related operations including deletion
 */
export class ThreadService {
  /**
   * Deletes a thread and all associated data (ACS thread + CosmosDB records)
   * @param threadId The ID of the thread to delete
   * @param threadTopic Optional topic name for better user feedback
   * @returns Promise<ThreadDeleteResult> with detailed result information
   */
  public static async deleteThread(threadId: string, threadTopic?: string): Promise<ThreadDeleteResult> {
    try {
      console.log(`[ThreadService] Deleting thread: ${threadId}`);
      
      // Call backend API to delete the thread and all associated data
      const response = await apiService.delete(`/chat/thread/${threadId}`);
      
      if (response.data?.success) {
        console.log(`[ThreadService] Successfully deleted thread ${threadId} from backend (ACS + CosmosDB)`);
        return {
          success: true,
          threadId,
          message: threadTopic 
            ? `Conversation "${threadTopic}" has been successfully deleted`
            : 'Conversation has been successfully deleted'
        };
      } else {
        console.warn(`[ThreadService] Backend delete completed but with warnings for thread ${threadId}`);
        return {
          success: true, // Still consider it successful for UI purposes
          threadId,
          message: threadTopic 
            ? `Conversation "${threadTopic}" has been successfully deleted`
            : 'Conversation has been successfully deleted'
        };
      }
    } catch (error) {
      console.error(`[ThreadService] Failed to delete thread ${threadId}:`, error);
      
      // Check if it's a 404 (thread not found) - consider this a success for cleanup
      if ((error as any)?.response?.status === 404) {
        console.log(`[ThreadService] Thread ${threadId} not found on backend, considering deletion successful`);
        return {
          success: true,
          threadId,
          message: threadTopic 
            ? `Conversation "${threadTopic}" has been successfully removed`
            : 'Conversation has been successfully removed'
        };
      }
      
      // For other errors, return failure result
      return {
        success: false,
        threadId,
        message: threadTopic 
          ? `Failed to delete conversation "${threadTopic}". Please try again.`
          : 'Failed to delete thread. Please try again.'
      };
    }
  }
}

// Export as default for convenience
export default ThreadService;
