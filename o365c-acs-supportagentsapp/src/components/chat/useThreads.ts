// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { useState, useEffect, useMemo, useRef } from 'react';
import { ChatClient, ChatMessageReceivedEvent, ChatThreadItem } from '@azure/communication-chat';
import { AzureCommunicationTokenCredential, CommunicationUserKind } from '@azure/communication-common';
import { getNextActiveThreadId } from '../../utils/threadsUtils';
import { AgentWorkItem, getAgentWorkItems, deleteAgentWorkItem } from '../../services/workItemService';

export interface ThreadItem {
  id: string;
  topic: string;
  lastMessageReceivedOn: Date;
  status?: ThreadItemStatus;
  priority?: string;
}

export enum ThreadItemStatus {
  UNASSIGNED = 0,
  CLAIMED = 1,
  ACTIVE = 2,
  RESOLVED = 3,
  CANCELLED = 4
}

interface UseThreadsProps {
  userId: string;
  token: string;
  endpointUrl: string;
}

interface UseThreadsReturn {
  threads: ThreadItem[];
  setThreads: React.Dispatch<React.SetStateAction<ThreadItem[]>>;
  selectedThreadId?: string;
  setSelectedThreadId: React.Dispatch<React.SetStateAction<string | undefined>>;
  resolvedThreadId?: string;
  setResolvedThreadId: React.Dispatch<React.SetStateAction<string | undefined>>;
  isLoading: boolean;
  deleteThread: (threadId: string) => Promise<void>;
}

export const useThreads = (props: UseThreadsProps): UseThreadsReturn => {
  const { userId, token, endpointUrl } = props;
  const [threads, setThreads] = useState<ThreadItem[]>([]);
  const [selectedThreadId, setSelectedThreadId] = useState<string | undefined>(undefined);
  const [resolvedThreadId, setResolvedThreadId] = useState<string | undefined>(undefined);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const hasFetchedInitially = useRef(false);

  const chatClient = useMemo(() => {
    if (!endpointUrl) {
      return;
    }
    const createChatClient = async (): Promise<ChatClient | undefined> => {
      if (!token) {
        return;
      }
      const tokenCredential = new AzureCommunicationTokenCredential(token);
      const chatClient = new ChatClient(endpointUrl, tokenCredential);
      return chatClient;
    };
    return createChatClient();
  }, [endpointUrl, token]);

  useEffect(() => {
    const addChatClientListeners = async (): Promise<void> => {
      try {
        const client = await chatClient;
        if (!client) {
          return;
        }
        await client.startRealtimeNotifications();

        client.on('participantsAdded', async (event) => {
          const participantsAdded = event.participantsAdded;
          const isCurrentUserAdded = participantsAdded.some((participant) => {
            const participantId = participant.id as CommunicationUserKind;
            return participantId.communicationUserId === userId;
          });

          if (isCurrentUserAdded) {
            const topic = (await client.getChatThreadClient(event.threadId).getProperties()).topic;
            const threadItem: ThreadItem = {
              id: event.threadId,
              topic: topic,
              lastMessageReceivedOn: new Date(),
              status: ThreadItemStatus.ACTIVE
            };

            setThreads((prevThreads: ThreadItem[]) => {
              const existingThreadIndex = prevThreads.findIndex((thread) => thread.id === threadItem.id);
              if (existingThreadIndex === -1) {
                return [threadItem, ...prevThreads];
              }
              return prevThreads;
            });
          }
        });

        client.on('participantsRemoved', async (event) => {
          const threadId = event.threadId;
          setThreads((prevThreads: ThreadItem[]) => {
            const threadIndex = prevThreads.findIndex((thread) => thread.id === threadId);
            if (threadIndex === -1) {
              console.error(`Participant removed from unknown thread: ${threadId}`);
              return prevThreads;
            }
            const [updatedThread] = prevThreads.splice(threadIndex, 1);
            updatedThread.status = ThreadItemStatus.RESOLVED;
            const newThreads = [updatedThread, ...prevThreads];
            newThreads.sort(
              (a: ThreadItem, b: ThreadItem) => b.lastMessageReceivedOn.getTime() - a.lastMessageReceivedOn.getTime()
            );
            return newThreads;
          });

          if (selectedThreadId === threadId) {
            const nextActiveThreadId = getNextActiveThreadId(threads, threadId);
            setSelectedThreadId(nextActiveThreadId);
          }
          setResolvedThreadId(threadId);
        });

        client.on('chatMessageReceived', (event: ChatMessageReceivedEvent) => {
          const threadId = event.threadId;
          setThreads((prevThreads: ThreadItem[]) => {
            const threadIndex = prevThreads.findIndex((thread) => thread.id === threadId);
            if (threadIndex === -1) {
              console.error(`Received message for unknown thread: ${threadId}`);
              return prevThreads;
            }
            const [updatedThread] = prevThreads.splice(threadIndex, 1);
            updatedThread.lastMessageReceivedOn = new Date();
            if (
              updatedThread.status === ThreadItemStatus.RESOLVED &&
              (event.sender as CommunicationUserKind).communicationUserId !== userId
            ) {
              updatedThread.status = ThreadItemStatus.ACTIVE;
            }
            return [updatedThread, ...prevThreads];
          });
        });
      } catch (error) {
        console.error('Failed to add listeners because client is not initialized');
      }
    };
    addChatClientListeners();
  }, [chatClient, selectedThreadId, threads, userId]);

  useEffect(() => {
    const fetchThreads = async (): Promise<void> => {
      setIsLoading(true);
      try {
        console.log('🔍 Starting to fetch threads...');
        console.log('🔑 Current userId:', userId);
        console.log('🌐 ACS endpointUrl:', endpointUrl);
        console.log('🎫 Token length:', token?.length || 0);
        
        const client = await chatClient;
        if (!client) {
          console.log('❌ Chat client is not initialized');
          setThreads([]);
          setIsLoading(false);
          return;
        }

        console.log('📞 Calling client.listChatThreads()...');
        const threadsResponse = await client.listChatThreads().byPage().next();
        const acsThreads = threadsResponse.value;
        console.log('📋 Received threads from ACS:', acsThreads?.length || 0);

        console.log('📞 Calling getAgentWorkItems() to get all work items...');
        const allWorkItems = await getAgentWorkItems();
        console.log('📝 Received total work items:', allWorkItems?.length || 0);

        // Debug: Log all work items with their status and assignment
        allWorkItems.forEach((item: AgentWorkItem) => {
          console.log(`📋 Work item ${item.id.substring(0, 20)}... - Status: ${item.status}, AssignedTo: ${item.assignedAgentId?.substring(0, 20)}...`);
        });

        // Filter to get only work items assigned to this agent (all statuses except unassigned)
        const myWorkItems = allWorkItems.filter((item: AgentWorkItem) =>
          item.assignedAgentId === userId &&
          (item.status === ThreadItemStatus.CLAIMED ||
           item.status === ThreadItemStatus.ACTIVE ||
           item.status === ThreadItemStatus.RESOLVED)
        );
        console.log('👤 My work items (claimed/active/resolved):', myWorkItems?.length || 0);

        // Build thread list from work items (primary source of truth)
        // This ensures threads persist across page refreshes
        const validThreadItems: ThreadItem[] = [];

        for (const workItem of myWorkItems) {
          // Try to get thread details from ACS if available
          let topic = workItem.customerName || 'Chat';
          let lastMessageReceivedOn = new Date(workItem.updatedAt);

          const acsThread = acsThreads?.find((thread: ChatThreadItem) => thread.id === workItem.id);
          if (acsThread) {
            topic = acsThread.topic;
            lastMessageReceivedOn = acsThread.lastMessageReceivedOn;
            console.log(`✅ Thread ${workItem.id}: found in ACS threads`);
          } else {
            console.log(`⚠️ Thread ${workItem.id}: not in ACS threads yet (using work item data)`);
          }

          // Map CLAIMED status (1) to ACTIVE status (2) for UI display
          // CLAIMED is the DB state when agent accepts, but UI shows it as ACTIVE
          const displayStatus = workItem.status === ThreadItemStatus.CLAIMED
            ? ThreadItemStatus.ACTIVE
            : workItem.status;

          const threadItem: ThreadItem = {
            id: workItem.id,
            topic: topic,
            lastMessageReceivedOn: lastMessageReceivedOn,
            status: displayStatus,
            priority: (workItem as any).priority || 'NORMAL'
          };
          validThreadItems.push(threadItem);
        }

        // Sort by last message time
        validThreadItems.sort((a: ThreadItem, b: ThreadItem) => {
          const aTime = a.lastMessageReceivedOn?.getTime() || 0;
          const bTime = b.lastMessageReceivedOn?.getTime() || 0;
          return bTime - aTime;
        });

        console.log('✅ Final valid thread items:', validThreadItems.length);
        setThreads(validThreadItems);
      } catch (error) {
        console.error('❌ Failed to fetch threads:', error);
        setThreads([]);
      }
      setIsLoading(false);
    };
    
    if (token && userId && endpointUrl) {
      // Only fetch on initial load, not on every chatClient change
      // SignalR will handle real-time updates
      if (!hasFetchedInitially.current) {
        console.log('🎯 useThreads: First time fetch - calling fetchThreads()');
        hasFetchedInitially.current = true;
        fetchThreads();
      } else {
        console.log('⏭️ useThreads: Already fetched initially, skipping fetch');
      }
    }
  }, [token, userId, endpointUrl]);  // Removed chatClient dependency to prevent constant re-fetching

  const deleteThread = async (threadId: string): Promise<void> => {
    try {
      console.log(`🗑️ Deleting thread: ${threadId}`);
      
      // Delete from work items service
      await deleteAgentWorkItem(threadId);
      
      // Remove from local state
      setThreads(prevThreads => prevThreads.filter(thread => thread.id !== threadId));
      
      // If the deleted thread was selected, select next available thread
      if (selectedThreadId === threadId) {
        const remainingThreads = threads.filter(thread => thread.id !== threadId);
        const nextActiveThread = remainingThreads.find(thread => thread.status === ThreadItemStatus.ACTIVE);
        setSelectedThreadId(nextActiveThread?.id);
      }
      
      console.log(`✅ Successfully deleted thread: ${threadId}`);
    } catch (error) {
      console.error('❌ Failed to delete thread:', error);
      throw error;
    }
  };

  return {
    threads,
    setThreads,
    selectedThreadId,
    setSelectedThreadId,
    resolvedThreadId,
    setResolvedThreadId,
    isLoading,
    deleteThread
  };
};
