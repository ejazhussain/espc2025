// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { ThreadList } from './ThreadList';
import { ChatScreen } from './ChatScreen';
import { IncomingQueuePanel } from './IncomingQueuePanel';
import { CancelledItemsPanel } from './CancelledItemsPanel';
import { getToken } from '../../services/authService';
import { getEndpointUrl } from '../../services/endpointService';
import { TeamsFxContext } from '../Context';
import { useData } from '@microsoft/teamsfx-react';
import { AgentUser, getAgentUser } from '../../services/userService';
import { ThreadItemStatus, useThreads } from './useThreads';
import { threadStrings } from '../../constants/constants';
import { ErrorScreen } from './ErrorScreen';
import { getNextActiveThreadId } from '../../utils/threadsUtils';
import { ToastNotification } from './ToastNotification';
import { ShareChatDialog } from './ShareChatDialog';
import { updateAgentWorkItem, getCancelledWorkItems } from '../../services';
import { AgentWorkItem } from '../../services/workItemService';
import { useSignalR } from '../../hooks/useSignalR';
import { getSignalRApiBaseUrl } from '../../lib/config';

export const AgentScreen = ({ initialThreadId }: { initialThreadId?: string }): JSX.Element => {
  const [token, setToken] = useState('');
  const [userId, setUserId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [endpointUrl, setEndpointUrl] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);
  const tabs = useMemo(
    () => ["Incoming Queue", "Active", "Resolved", "Cancelled"],
    []
  );
  const [selectedTab, setSelectedTab] = useState<string>(tabs[0]);
  const [isShareDialogOpen, setIsShareDialogOpen] = useState(false);
  const [shareThreadId, setShareThreadId] = useState<string>('');
  const [shareCustomerName, setShareCustomerName] = useState<string>('');
  const [unassignedItems, setUnassignedItems] = useState<AgentWorkItem[]>([]);
  const [cancelledItems, setCancelledItems] = useState<AgentWorkItem[]>([]);
  const {
    threads,
    setThreads,
    selectedThreadId,
    setSelectedThreadId,
    resolvedThreadId,
    setResolvedThreadId,
    isLoading,
    deleteThread
  } = useThreads({
    userId,
    token,
    endpointUrl
  });
  const { teamsUserCredential, themeString } = useContext(TeamsFxContext);

  // Initialize SignalR connection for real-time notifications
  const signalRApiBaseUrl = getSignalRApiBaseUrl();
  const { connection: signalRConnection, isConnected: signalRConnected } = useSignalR(signalRApiBaseUrl);

  const { loading, data, error } = useData(async () => {
    if (teamsUserCredential) {
      try {
        const userInfo = await teamsUserCredential.getUserInfo();
        return userInfo;
      } catch (error) {
        console.error('Failed to get user info: ', error);
        setErrorMessage(threadStrings.failToGetTeamsUserInfo);
      }
    }
  });

  const getACSUser = useCallback(async (teamsUserId: string): Promise<AgentUser | undefined> => {
    if (!teamsUserId) {
      return undefined;
    }
    try {
      const agentACSUser = await getAgentUser(teamsUserId);
      return agentACSUser;
    } catch (error) {
      console.error('Failed to get ACS user:', error);
      throw error;
    }
  }, []);

  useEffect(() => {
    // Wait for Teams user data to be loaded
    if (loading || error || !data?.objectId) {
      return;
    }

    (async () => {
      try {
        const teamsUserId = data.objectId;
        const agentACSUser = await getACSUser(teamsUserId);
        if (!agentACSUser) {
          setErrorMessage(threadStrings.failToLinkToACSUser);
          return;
        }
        const endpointUrl = await getEndpointUrl();
        const tokenResponse = await getToken(agentACSUser.acsUserId);
        const displayName = agentACSUser.displayName;

        setUserId(agentACSUser.acsUserId);
        setEndpointUrl(endpointUrl);
        setToken(tokenResponse.token);
        setDisplayName(displayName);
        // Thread selection is now handled in a separate useEffect below
      } catch (error) {
        console.error('Failed to set screen state due to error: ', error);
        setErrorMessage(threadStrings.failToLinkToACSUser);
      }
    })();
  }, [getACSUser, data, loading, error]);
// Select initial thread after threads are loaded
  useEffect(() => {
    if (!initialThreadId || !Array.isArray(threads) || threads.length === 0) return;
    const activeThread = threads.find(
      (thread: any) => thread.id === initialThreadId && thread.status === ThreadItemStatus.ACTIVE
    );
    if (activeThread) {
      setSelectedThreadId(initialThreadId);
    } else {
      // Fallback to first active thread
      const firstActiveThread = threads.find(
        (thread: any) => thread.status === ThreadItemStatus.ACTIVE
      );
      setSelectedThreadId(firstActiveThread?.id);
    }
  }, [threads, initialThreadId]);

  // Global SignalR listeners - always listening regardless of selected tab
  useEffect(() => {
    if (!signalRConnection || !signalRConnected) return;

    const handleNewChatRequest = (data: any) => {
      console.log('[AgentScreen] SignalR newChatRequest received:', data);
      console.log('[AgentScreen] Data type:', typeof data);

      try {
        // If data is a string, parse it
        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;
        console.log('[AgentScreen] Parsed data:', parsedData);

        if (parsedData.workItem) {
          // Add to unassigned items queue
          setUnassignedItems((prev) => [parsedData.workItem, ...prev]);

          // Optional: Show notification or play sound
          console.log('[AgentScreen] New chat request added to queue:', parsedData.workItem.id);
        } else {
          console.error('[AgentScreen] No workItem in data! Full data:', parsedData);
        }
      } catch (error) {
        console.error('[AgentScreen] Error parsing SignalR data:', error, data);
      }
    };

    const handleChatClaimed = (data: any) => {
      console.log('[AgentScreen] SignalR chatClaimed received:', data);

      try {
        // If data is a string, parse it
        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;
        console.log('[AgentScreen] Parsed chatClaimed data:', parsedData);

        if (parsedData.threadId) {
          // Remove from unassigned items queue
          setUnassignedItems((prev) => prev.filter((item) => item.id !== parsedData.threadId));
          console.log('[AgentScreen] Chat claimed, removed from queue:', parsedData.threadId);
        }
      } catch (error) {
        console.error('[AgentScreen] Error parsing chatClaimed data:', error, data);
      }
    };

    const handleWorkItemCancelled = (data: any) => {
      console.log('[AgentScreen] SignalR workItemCancelled received:', data);

      try {
        // If data is a string, parse it
        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;
        console.log('[AgentScreen] Parsed workItemCancelled data:', parsedData);

        if (parsedData.threadId) {
          const threadId = parsedData.threadId;
          
          // Check if item is in unassigned queue
          setUnassignedItems((prev) => {
            const cancelledItem = prev.find(item => item.id === threadId);
            
            if (cancelledItem) {
              console.log('[AgentScreen] Found item in Unassigned, moving to Cancelled:', threadId);
              
              // Add to cancelled items with updated status
              const updatedItem: AgentWorkItem = {
                ...cancelledItem,
                status: ThreadItemStatus.CANCELLED
              };
              
              setCancelledItems((prevCancelled) => [updatedItem, ...prevCancelled]);
            }
            
            // Remove from unassigned queue
            return prev.filter((item) => item.id !== threadId);
          });
          
          // Check if this is the currently selected active thread
          const isCurrentlySelected = selectedThreadId === threadId;
          
          // Also check if it's an active thread and update its status to RESOLVED
          setThreads((prev) => {
            const activeThread = prev.find(thread => thread.id === threadId && thread.status === ThreadItemStatus.ACTIVE);
            
            if (activeThread) {
              console.log('[AgentScreen] Found active thread, marking as RESOLVED:', threadId);
              
              // If this was the selected thread, find the next active thread
              if (isCurrentlySelected) {
                const otherActiveThreads = prev.filter(
                  thread => thread.id !== threadId && thread.status === ThreadItemStatus.ACTIVE
                );
                
                if (otherActiveThreads.length > 0) {
                  // Switch to the next active thread
                  const nextThread = otherActiveThreads[0];
                  console.log('[AgentScreen] Switching to next active thread:', nextThread.id);
                  setSelectedThreadId(nextThread.id);
                } else {
                  // No more active threads, clear selection
                  console.log('[AgentScreen] No more active threads, clearing selection');
                  setSelectedThreadId(undefined);
                }
              }
              
              // Update thread status to RESOLVED (customer ended chat)
              return prev.map(thread => 
                thread.id === threadId 
                  ? { ...thread, status: ThreadItemStatus.RESOLVED }
                  : thread
              );
            }
            
            console.log('[AgentScreen] Thread not found in active threads:', threadId);
            return prev;
          });
        }
      } catch (error) {
        console.error('[AgentScreen] Error parsing workItemCancelled data:', error, data);
      }
    };

    // Subscribe to SignalR events
    signalRConnection.on('newChatRequest', handleNewChatRequest);
    signalRConnection.on('chatClaimed', handleChatClaimed);
    signalRConnection.on('workItemCancelled', handleWorkItemCancelled);

    console.log('[AgentScreen] SignalR listeners registered');

    // Cleanup on unmount
    return () => {
      signalRConnection.off('newChatRequest', handleNewChatRequest);
      signalRConnection.off('chatClaimed', handleChatClaimed);
      signalRConnection.off('workItemCancelled', handleWorkItemCancelled);
      console.log('[AgentScreen] SignalR listeners unregistered');
    };
  }, [signalRConnection, signalRConnected]);

  // Fetch cancelled work items when Cancelled tab is selected
  useEffect(() => {
    if (selectedTab === "Cancelled") {
      const fetchCancelledItems = async () => {
        try {
          const items = await getCancelledWorkItems();
          setCancelledItems(items);
          console.log('[AgentScreen] Fetched cancelled work items:', items.length);
        } catch (error) {
          console.error('[AgentScreen] Failed to fetch cancelled work items:', error);
        }
      };
      fetchCancelledItems();
    }
  }, [selectedTab]);

  const handleOnResolveChat = useCallback(
    async (threadId: string) => {
      setThreads((prevThreads: any) =>
        prevThreads.map((thread: any) => {
          if (thread.id === threadId) {
            return { ...thread, status: ThreadItemStatus.RESOLVED };
          }
          return thread;
        })
      );

      // Update backend work item status to RESOLVED
      try {
        await updateAgentWorkItem(threadId, ThreadItemStatus.RESOLVED);
        console.log('✅ Backend work item updated to RESOLVED');
      } catch (error) {
        console.error('❌ Failed to update backend work item:', error);
        // Maybe revert UI state on failure
      }

      const nextActiveThreadId = getNextActiveThreadId(threads, threadId);
      setSelectedThreadId(nextActiveThreadId);
    },
    [setSelectedThreadId, setThreads, threads]
  );

  const handleOnShareChat = useCallback(() => {
    if (selectedThreadId) {
      const thread = threads?.find((thread: any) => thread.id === selectedThreadId);
      setShareThreadId(selectedThreadId);
      setShareCustomerName(thread?.topic || 'Customer');
      setIsShareDialogOpen(true);
    }
  }, [selectedThreadId, threads]);

  const chatScreen = useCallback(() => {
    if (!selectedThreadId || !token || !endpointUrl || !userId || !displayName) {
      return <></>;
    }
    const thread = threads?.find((thread: any) => thread.id === selectedThreadId);
    return (
      <ChatScreen
        token={token}
        userId={userId}
        displayName={displayName}
        endpointUrl={endpointUrl}
        threadId={selectedThreadId}
        receiverName={thread?.topic || ''}
        threadStatus={thread?.status || ThreadItemStatus.ACTIVE}
        onResolveChat={handleOnResolveChat}
        onShareChat={handleOnShareChat}
      />
    );
  }, [selectedThreadId, token, endpointUrl, userId, displayName, threads, handleOnResolveChat]);

  const resolvedThreadCustomerDisplayName = useMemo(() => {
    if (!resolvedThreadId) {
      return;
    }
    const resolvedThread = threads.find((thread: any) => thread.id === resolvedThreadId);
    return resolvedThread?.topic;
  }, [resolvedThreadId, threads]);

  const handleOnViewThread = useCallback(
    (threadId: string) => {
      setSelectedThreadId(threadId);
      //Change tab to active
      setSelectedTab(tabs[1]);
      if (resolvedThreadId === threadId) {
        setResolvedThreadId(undefined);
      }
    },
    [resolvedThreadId, setResolvedThreadId, setSelectedThreadId, tabs]
  );

  const handleOnStatusTabSelected = useCallback(
    (tabValue: string) => {
      setSelectedTab(tabValue);

      // Don't select a thread when switching to Incoming Queue tab
      if (tabValue === "Incoming Queue") {
        setSelectedThreadId(undefined);
        return;
      }

      // Convert tab name to status value
      const statusValue = tabValue.toLowerCase() === 'active' ? ThreadItemStatus.ACTIVE : ThreadItemStatus.RESOLVED;
      const firstThreadOfSelectedTab = threads.find((thread: any) => thread.status === statusValue);
      setSelectedThreadId(firstThreadOfSelectedTab?.id);
    },
    [setSelectedThreadId, threads]
  );

  const handleChatAccepted = useCallback(
    async (threadId: string, workItem: AgentWorkItem) => {
      console.log('Chat accepted:', threadId, workItem);

     
      const newThread = {
        id: workItem.id,
        topic: workItem.customerName || 'New Chat',
        lastMessageReceivedOn: new Date(workItem.createdAt),
        status: ThreadItemStatus.ACTIVE, // Show as ACTIVE in the UI
        priority: workItem.priority || 'NORMAL'
      };

      setThreads((prevThreads: any) => [newThread, ...prevThreads]);

      // Select the new thread
      setSelectedThreadId(threadId);

      // Switch to Active tab
      setSelectedTab("Active");
    },
    [setThreads, setSelectedThreadId, setSelectedTab]
  );

  const isDark = themeString === 'dark';
  
  // Debug: Log theme information
  console.log('Theme debug:', { themeString, isDark });

  return (
    <div className={`h-screen ${isDark ? 'bg-gray-900' : 'bg-gray-50'}`}>
      {errorMessage ? (
        <ErrorScreen errorMessage={errorMessage} isDark={isDark} />
      ) : (
        <div className={`h-full flex ${isDark ? 'bg-gray-900' : 'bg-gray-50'}`}>
          {resolvedThreadId && (
            <ToastNotification
              toasterId={resolvedThreadId}
              showToast={!!resolvedThreadId}
              toastBodyMessage={resolvedThreadCustomerDisplayName}
              onViewThread={handleOnViewThread}
            />
          )}

          {/* Left Panel - Tabs with Incoming Queue, Active, Resolved, Cancelled */}
          <div className={`w-[28%] min-w-80 ${isDark ? 'bg-gray-900 border-r border-gray-700' : 'bg-gray-50 border-r border-gray-200'}`}>
            {selectedTab === "Incoming Queue" ? (
              <IncomingQueuePanel
                onChatAccepted={handleChatAccepted}
                currentAgentId={userId}
                currentAgentName={displayName}
                isDark={isDark}
                tabs={tabs}
                selectedTab={selectedTab}
                onStatusTabSelected={handleOnStatusTabSelected}
                signalRConnection={signalRConnection}
                signalRConnected={signalRConnected}
                unassignedItems={unassignedItems}
                setUnassignedItems={setUnassignedItems}
              />
            ) : selectedTab === "Cancelled" ? (
              <CancelledItemsPanel
                cancelledItems={cancelledItems}
                tabs={tabs}
                selectedTab={selectedTab}
                onStatusTabSelected={handleOnStatusTabSelected}
                isDark={isDark}
              />
            ) : (
              <ThreadList
                selectedThreadId={selectedThreadId}
                onThreadSelected={setSelectedThreadId}
                threads={threads}
                isLoading={!endpointUrl || isLoading}
                tabs={tabs}
                selectedTab={selectedTab}
                onStatusTabSelected={handleOnStatusTabSelected}
                isDark={isDark}
                onDeleteThread={deleteThread}
                incomingQueueCount={unassignedItems.length}
              />
            )}
          </div>

          {/* Right Panel - Chat Window */}
          <div className={`flex-1 ${isDark ? 'bg-gray-900' : 'bg-white'}`}>
            {chatScreen()}
          </div>
        </div>
      )}
      
      {/* Share Chat Dialog */}
      <ShareChatDialog
        isOpen={isShareDialogOpen}
        threadId={shareThreadId}
        customerName={shareCustomerName}
        onClose={() => setIsShareDialogOpen(false)}
        isDark={isDark}
      />
    </div>
  );
};
