// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import {
  ChatClientProvider,
  ChatThreadClientProvider,
  createStatefulChatClient,
} from '@azure/communication-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ChatHeader } from './ChatHeader';
import { strings } from './utils/constants';
import { LoadingSpinner } from './LoadingSpinner';
import ChatComponents from './ChatComponents';
import { 
  ChatThreadClient, 
  ChatThreadPropertiesUpdatedEvent,
} from '@azure/communication-chat';
import AzureCommunicationService, { ThreadItemStatus } from '../services/AzureCommunicationService';
import { useSignalR } from '../hooks/useSignalR';
import { DEFAULT_API_CONFIG } from '../config/apiConfig';

// These props are passed in when this component is referenced in JSX and not found in context
interface ChatScreenProps {
  token: string;
  userId: string;
  displayName: string;
  endpointUrl: string;
  threadId: string;
  agentName: string;
  onEndChat(chatThreadClient: ChatThreadClient): void;
}

export const ChatScreen = (props: ChatScreenProps): JSX.Element => {
  const { displayName, endpointUrl, threadId, token, userId, agentName, onEndChat } = props;
  const [chatThreadClient, setChatThreadClient] = useState<ChatThreadClient | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [isResolvedByAgent, setIsResolvedByAgent] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [isAgentConnected, setIsAgentConnected] = useState(false);
  
  // Initialize ACS service for agent work item management
  const acsService = useMemo(() => new AzureCommunicationService(), []);
  
  // Initialize SignalR connection for real-time notifications
  const { connection: signalRConnection, isConnected: signalRConnected } = useSignalR(DEFAULT_API_CONFIG.signalRBaseURL);
  
  // Disables pull down to refresh. Prevents accidental page refresh when scrolling through chat messages
  // Another alternative: set body style touch-action to 'none'. Achieves same result.
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = 'null';
    };
  }, []);

  // Instantiate the statefulChatClient
  const statefulChatClient = useMemo(() => {
    const tokenCredential = new AzureCommunicationTokenCredential(token);
    const chatClient = createStatefulChatClient({
      userId: { communicationUserId: userId },
      displayName: displayName,
      endpoint: endpointUrl,
      credential: tokenCredential
    });
    return chatClient;
  }, [displayName, endpointUrl, token, userId]);

  useEffect(() => {
    const initializeChatThreadClient = async (): Promise<void> => {
      console.log('[Chat Screen] Initializing chat thread client with params:', {
        threadId,
        userId,
        displayName,
        endpointUrl: endpointUrl?.substring(0, 50) + '...',
        hasToken: !!token,
        hasStatefulClient: !!statefulChatClient
      });

      setIsLoading(true);
      setConnectionError(null);
      
      if (!statefulChatClient) {
        console.error('[Chat Screen] Stateful chat client is not available');
        setConnectionError("Chat client initialization failed. Please refresh the page.");
        setIsLoading(false);
        return;
      }

      if (!threadId) {
        console.error('[Chat Screen] Thread ID is missing');
        setConnectionError("Chat thread ID is missing. Please restart the chat.");
        setIsLoading(false);
        return;
      }
      
      console.log('[Chat Screen] Creating thread client for thread:', threadId);
      const threadClient = statefulChatClient.getChatThreadClient(threadId);
      
      try {
        console.log('[Chat Screen] Attempting to get thread properties...');
        const properties = await threadClient.getProperties();
        console.log('[Chat Screen] Successfully retrieved thread properties:', {
          topic: properties.topic,
          createdOn: properties.createdOn,
          createdBy: properties.createdBy?.kind
        });
        setChatThreadClient(threadClient);
        console.log('[Chat Screen] Chat thread client initialized successfully');
      } catch (error) {
        console.error('[Chat Screen] Failed to initialize chat thread client:', error);
        
        // Type-safe error handling
        const errorDetails = {
          errorType: error?.constructor?.name || 'Unknown',
          message: (error as any)?.message || 'No error message',
          stack: (error as any)?.stack?.substring(0, 500) || 'No stack trace'
        };
        console.error('[Chat Screen] Error details:', errorDetails);
        
        // Check if it's a permissions issue
        const errorMessage = (error as any)?.message || '';
        if (errorMessage.includes('Forbidden') || errorMessage.includes('403')) {
          console.error('[Chat Screen] 403 Forbidden error - user may not be a participant in the thread');
          setConnectionError("You don't have permission to access this chat. This may be a temporary issue - please try refreshing the page in a few seconds.");
        } else {
          setConnectionError("Unable to connect to the chat service. Please try refreshing the page, or contact support if the issue persists.");
        }
      } finally {
        setIsLoading(false);
      }
    };

    initializeChatThreadClient();
  }, [statefulChatClient, threadId, userId, displayName, endpointUrl, token]);

  // SignalR listeners for real-time chat status updates
  useEffect(() => {
    if (!signalRConnection || !signalRConnected || !threadId) {
      return;
    }

    console.log('[Customer SignalR] Setting up event listeners for thread:', threadId);

    const handleChatClaimed = (data: any) => {
      try {
        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;

        // Only process events for this specific thread
        if (parsedData.threadId === threadId) {
          console.log('[Customer ChatScreen] ✅ Agent has accepted your chat! (via SignalR)');
          setIsAgentConnected(true);
        }
      } catch (error) {
        console.error('[Customer SignalR] Error parsing chatClaimed data:', error, data);
      }
    };

    const handleWorkItemCancelled = (data: any) => {
      try {
        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;

        // Only process events for this specific thread
        if (parsedData.threadId === threadId) {
          console.log('[Customer ChatScreen] ❌ Agent has cancelled your chat request');
          setConnectionError('The agent cancelled this chat request. Please try again.');
        }
      } catch (error) {
        console.error('[Customer SignalR] Error parsing workItemCancelled data:', error, data);
      }
    };

    // Subscribe to SignalR events
    // Note: Azure SignalR Service converts method names to lowercase
    signalRConnection.on('chatclaimed', handleChatClaimed);
    signalRConnection.on('workitemcancelled', handleWorkItemCancelled);

    console.log('[Customer SignalR] Subscribed to chat events for thread:', threadId);

    // Cleanup on unmount
    return () => {
      signalRConnection.off('chatclaimed', handleChatClaimed);
      signalRConnection.off('workitemcancelled', handleWorkItemCancelled);
      console.log('[Customer SignalR] Unsubscribed from chat events');
    };
    // Note: isAgentConnected is intentionally NOT in the dependency array
    // We only want to set up event listeners once when SignalR connects
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [signalRConnection, signalRConnected, threadId]);

  // Check for agent acceptance when thread client is ready
  // SignalR is primary notification method, polling is backup for reliability
  useEffect(() => {
    // If agent is already connected, no need to check
    if (isAgentConnected) {
      console.log('[ChatScreen] Agent already connected, skipping acceptance check');
      return;
    }

    // If SignalR is connected, use longer polling interval as backup only
    const pollingInterval = signalRConnected ? 10000 : 5000; // 10s with SignalR, 5s without
    
    const checkForAgentAcceptance = async () => {
      if (!threadId || isAgentConnected) return;
      
      try {
        const isAccepted = await acsService.isAgentAccepted(threadId);
        
        if (isAccepted) {
          const method = signalRConnected ? 'polling backup' : 'polling';
          console.log(`[ChatScreen] ✅ Agent accepted (via ${method})!`);
          setIsAgentConnected(true);
        }
      } catch (error) {
        console.error('[ChatScreen] Error checking agent acceptance:', error);
      }
    };
    
    // Initial check
    checkForAgentAcceptance();
    
    // Set up polling interval (faster if no SignalR, slower as backup with SignalR)
    const intervalId = setInterval(checkForAgentAcceptance, pollingInterval);
    
    console.log(`[ChatScreen] Polling for agent acceptance every ${pollingInterval/1000}s (SignalR: ${signalRConnected ? 'active' : 'inactive'})`);
    
    return () => {
      clearInterval(intervalId);
    };
  }, [threadId, acsService, isAgentConnected, signalRConnected]);

  useEffect(() => {
    const addChatClientListeners = async (): Promise<void> => {
      if (!statefulChatClient) {
        console.error('Failed to add listeners because client is not initialized');
        return;
      }
      await statefulChatClient.startRealtimeNotifications();

      // Listen for thread properties updates (like when agent joins or resolves)
      statefulChatClient.on('chatThreadPropertiesUpdated', async (event: ChatThreadPropertiesUpdatedEvent) => {
        const { threadId: resolvedThreadId, properties } = event;
        
        // Check if thread is resolved
        if (!isResolvedByAgent && resolvedThreadId === threadId && properties.metadata?.isResolvedByAgent === 'true') {
          setIsResolvedByAgent(true);
        }
        
        // When properties update, re-check if agent has accepted
        if (resolvedThreadId === threadId) {
          try {
            const isAccepted = await acsService.isAgentAccepted(threadId);
            if (isAccepted && !isAgentConnected) {
              console.log('[ChatScreen] Agent just accepted the chat (detected via properties update)');
              setIsAgentConnected(true);
            }
          } catch (error) {
            console.error('[ChatScreen] Error checking agent acceptance on properties update:', error);
          }
        }
      });
    };
    addChatClientListeners();
  }, [statefulChatClient, isResolvedByAgent, threadId, acsService, isAgentConnected]);

  const handleOnResumeConversation = useCallback(async () => {
    try {
      setIsResolvedByAgent(false);
      await acsService.updateAgentWorkItem(threadId, ThreadItemStatus.ACTIVE);
      console.log('[ChatScreen] Successfully resumed conversation');
    } catch (error) {
      console.error('[ChatScreen] Failed to resume conversation:', error);
      // Don't prevent the UI update even if the backend call fails
      setIsResolvedByAgent(false);
    }
  }, [threadId, acsService]);

  return connectionError ? (
    <div className="h-screen max-h-[600px] w-full max-w-[600px] flex flex-col items-center justify-center bg-gray-50 border border-red-400 rounded-xl shadow-2xl p-6 text-center gap-4">
      <span className="text-2xl">⚠️</span>
      <h2 className="text-xl font-bold text-red-600">Connection Error</h2>
      <p className="text-base max-w-md leading-relaxed">{connectionError}</p>
    </div>
  ) : isLoading || !chatThreadClient ? (
    <div className="h-screen max-h-[600px] w-full max-w-[600px] flex items-center justify-center bg-gray-50 border border-gray-300 rounded-xl shadow-2xl">
      <LoadingSpinner label={strings.initializeChatSpinnerLabel} />
    </div>
  ) : (
    <div className="h-screen max-h-[600px] w-full max-w-[600px] flex flex-col bg-gradient-to-br from-primary-600 to-secondary-600 border border-gray-300 rounded-xl shadow-2xl overflow-hidden relative">
      {/* Chat Header */}
      <div className="relative z-10 bg-transparent text-white">
        <ChatHeader
          personaName={agentName}
          isAgentOnline={isAgentConnected && !isResolvedByAgent}
          isResolvedByAgent={isResolvedByAgent}
          onEndChat={() => {
            onEndChat(chatThreadClient);
          }}
        />
      </div>

      {/* Chat Content with clean white background */}
      <div className="flex-1 bg-white m-2 rounded-lg relative overflow-hidden flex flex-col">
        <div className="relative z-10 h-full flex flex-col">
          <ChatClientProvider chatClient={statefulChatClient}>
            <ChatThreadClientProvider chatThreadClient={chatThreadClient}>
              <ChatComponents 
                isResolvedByAgent={isResolvedByAgent}
                isAgentOnline={isAgentConnected && !isResolvedByAgent}
                onResumeConversation={handleOnResumeConversation}
                threadId={threadId}
                userId={userId}
                displayName={displayName}
              />
            </ChatThreadClientProvider>
          </ChatClientProvider>
        </div>
      </div>
    </div>
  );
};
