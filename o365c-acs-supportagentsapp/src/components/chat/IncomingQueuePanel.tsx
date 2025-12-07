// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useState, useEffect, useCallback } from 'react';
import { QueueItem } from './QueueItem';
import { AgentWorkItem, getUnassignedWorkItems, claimWorkItem, cancelWorkItem } from '../../services/workItemService';
import { ThreadItemStatus } from './useThreads';
import { ThreadListHeader } from './ThreadListHeader';
import { Inbox } from 'lucide-react';
import * as signalR from '@microsoft/signalr';

interface IncomingQueuePanelProps {
  onChatAccepted: (threadId: string, workItem: AgentWorkItem) => void;
  currentAgentId: string;
  currentAgentName: string;
  isDark: boolean;
  tabs: string[];
  selectedTab: string;
  onStatusTabSelected: (tabValue: string) => void;
  signalRConnection?: signalR.HubConnection | null;
  signalRConnected?: boolean;
  unassignedItems: AgentWorkItem[];
  setUnassignedItems: React.Dispatch<React.SetStateAction<AgentWorkItem[]>>;
}

export const IncomingQueuePanel: React.FC<IncomingQueuePanelProps> = ({
  onChatAccepted,
  currentAgentId,
  currentAgentName,
  isDark,
  tabs,
  selectedTab,
  onStatusTabSelected,
  signalRConnection,
  signalRConnected,
  unassignedItems,
  setUnassignedItems,
}) => {
  const [loading, setLoading] = useState(false);

  // Fetch unassigned work items on mount
  const fetchUnassignedItems = useCallback(async () => {
    try {
      setLoading(true);
      const items = await getUnassignedWorkItems();
      setUnassignedItems(items);
      console.log('[IncomingQueue] Fetched', items.length, 'unassigned items');
    } catch (error) {
      console.error('[IncomingQueue] Failed to fetch unassigned items:', error);
    } finally {
      setLoading(false);
    }
  }, [setUnassignedItems]);

  // Initial fetch
  useEffect(() => {
    fetchUnassignedItems();
  }, [fetchUnassignedItems]);

  // Note: SignalR listeners are now in AgentScreen (parent component)
  // This ensures real-time updates work regardless of which tab is selected

  // Handle accepting a chat - Real API call
  const handleAccept = async (threadId: string) => {
    try {
      console.log('[IncomingQueue] Claiming work item:', threadId);

      const result = await claimWorkItem(threadId, currentAgentId, currentAgentName);

      if (result.success && result.workItem) {
        console.log('[IncomingQueue] Successfully claimed:', threadId);

        // Remove from local queue (SignalR will also remove it)
        setUnassignedItems((prev) => prev.filter((item) => item.id !== threadId));

        // Notify parent to add to Active tab
        onChatAccepted(threadId, result.workItem);
      } else {
        // Already claimed by another agent
        console.warn('[IncomingQueue] Already claimed:', result.error);
        throw new Error(result.error || 'Failed to claim chat');
      }
    } catch (error: any) {
      console.error('[IncomingQueue] Failed to accept chat:', error);
      // Remove from queue even if error (likely already claimed)
      setUnassignedItems((prev) => prev.filter((item) => item.id !== threadId));
      throw error;
    }
  };

  // Handle canceling a chat request
  const handleCancel = async (threadId: string) => {
    try {
      console.log('[IncomingQueue] Canceling work item:', threadId);

      await cancelWorkItem(threadId);

      console.log('[IncomingQueue] Successfully canceled:', threadId);

      // Remove from local queue
      setUnassignedItems((prev) => prev.filter((item) => item.id !== threadId));
    } catch (error: any) {
      console.error('[IncomingQueue] Failed to cancel chat:', error);
      // Remove from queue anyway (might have been canceled by customer)
      setUnassignedItems((prev) => prev.filter((item) => item.id !== threadId));
      throw error;
    }
  };

  const bgColor = isDark ? 'bg-gray-900' : 'bg-gray-50';
  const textColor = isDark ? 'text-gray-100' : 'text-gray-900';
  const subtextColor = isDark ? 'text-gray-400' : 'text-gray-600';
  const borderColor = isDark ? 'border-gray-800' : 'border-gray-200';

  return (
    <div className={`h-full flex flex-col ${bgColor}`}>
      {/* Header with tabs - Match ThreadList styling */}
      <div className={`${isDark ? 'border-b border-gray-700' : 'border-b border-gray-100'} p-6`}>
        <ThreadListHeader
          tabs={tabs}
          selectedTab={selectedTab}
          onStatusTabSelected={onStatusTabSelected}
          isDark={isDark}
          incomingQueueCount={unassignedItems.length}
        />
      </div>

      {/* Queue Items */}
      <div className="flex-1 overflow-y-auto p-4">
        {unassignedItems.length === 0 ? (
          <div className="text-center py-8">
            <Inbox size={48} className={`mx-auto mb-3 ${subtextColor}`} />
            <p className={`text-lg font-medium ${textColor}`}>No incoming requests</p>
            <p className={`text-sm ${subtextColor} mt-2`}>New customer requests will appear here</p>
          </div>
        ) : (
          unassignedItems.map((item) => (
            <QueueItem 
              key={item.id} 
              workItem={item} 
              onAccept={handleAccept} 
              onCancel={handleCancel}
              isDark={isDark} 
            />
          ))
        )}
      </div>
    </div>
  );
};
