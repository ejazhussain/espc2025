// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import {
  ChatClientProvider,
  ChatThreadClientProvider,
  createStatefulChatClient,
  DEFAULT_COMPONENT_ICONS,
  FluentThemeProvider,
  lightTheme
} from '@azure/communication-react';
import { useCallback, useEffect, useMemo, useState } from 'react';

import { useChatScreenStyles } from '../../styles/ChatScreen.styles';
import { ThreadItemStatus } from './useThreads';
import { LoadingSpinner } from './LoadingSpinner';
import { ChatThreadClient } from '@azure/communication-chat';
import { initializeIcons, registerIcons } from '@fluentui/react';
import { useTeamsUserCredential } from '@microsoft/teamsfx-react';
import config from '../../lib/config';
import { updateAgentWorkItem } from '../../services/workItemService';
import ChatHeader from './ChatHeader';
import { v8DarkTheme } from '../../utils/themeUtils';
import { ChatComponents } from './ChatComponents';


// Register Fluent UI V8 icons so component icons, such as send button, can be displayed
initializeIcons();
registerIcons({ icons: DEFAULT_COMPONENT_ICONS });

// These props are passed in when this component is referenced in JSX and not found in context
interface ChatScreenProps {
  token: string;
  userId: string;
  displayName: string;
  endpointUrl: string;
  threadId: string;
  receiverName: string;
  threadStatus: ThreadItemStatus;
  onResolveChat(threadId: string): void;
  onShareChat?(): void;
}

export const ChatScreen = (props: ChatScreenProps): JSX.Element => {
  const { displayName, endpointUrl, threadId, token, userId, receiverName, threadStatus, onResolveChat, onShareChat } = props;
  const styles = useChatScreenStyles();
  const [chatThreadClient, setChatThreadClient] = useState<ChatThreadClient | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [isResolvingChat, setIsResolvingChat] = useState(false);
  const [agentEmail, setAgentEmail] = useState<string>('');

  // Access the current theme and user credential
  const { teamsUserCredential, themeString } = useTeamsUserCredential({
    initiateLoginEndpoint: config.initiateLoginEndpoint,
    clientId: config.clientId
  });

  // Get the logged-in user's email
  useEffect(() => {
    const getUserEmail = async () => {
      try {
        if (teamsUserCredential) {
          const userInfo = await teamsUserCredential.getUserInfo();
          setAgentEmail(userInfo.preferredUserName || '');
          console.log('[ChatScreen] Agent email:', userInfo.preferredUserName);
        }
      } catch (error) {
        console.error('[ChatScreen] Failed to get user email:', error);
      }
    };
    getUserEmail();
  }, [teamsUserCredential]);

  // Disables pull down to refresh. Prevents accidental page refresh when scrolling through chat messages
  // Another alternative: set body style touch-action to 'none'. Achieves same result.
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = 'null';
    };
  }, []);

  const handleOnResolveChat = useCallback(async () => {
    // Prevent duplicate calls if already resolving
    if (isResolvingChat) {
      console.log('Already resolving chat, ignoring duplicate call');
      return;
    }

    setIsResolvingChat(true);
    
    try {
      console.log(`🔄 Starting chat resolution for thread: ${threadId}`);
      
      // Update the chat thread metadata to notify the CustomerApp that the chat has been resolved
      await chatThreadClient?.updateProperties({ metadata: { isResolvedByAgent: 'true' } });
      
      // Update the agent work item status to resolved
      await updateAgentWorkItem(threadId, ThreadItemStatus.RESOLVED);
      
      // Notify parent component that chat was resolved
      onResolveChat(threadId);
      
      console.log(`✅ Successfully resolved chat for thread: ${threadId}`);
    } catch (error) {
      console.error('❌ Failed to resolve chat:', error);
    } finally {
      setIsResolvingChat(false);
    }
  }, [chatThreadClient, onResolveChat, threadId, isResolvingChat]);

  // Instantiate the statefulChatClient
  const statefulChatClient = useMemo(() => {
    const tokenCredential = new AzureCommunicationTokenCredential(token);
    const chatClient = createStatefulChatClient({
      userId: { communicationUserId: userId },
      displayName: displayName,
      endpoint: endpointUrl,
      credential: tokenCredential
    });
    chatClient.startRealtimeNotifications();

    return chatClient;
  }, [displayName, endpointUrl, token, userId]);

  useEffect(() => {
    const initializeChatThreadClient = async (): Promise<void> => {
      setIsLoading(true);
      const threadClient = statefulChatClient.getChatThreadClient(threadId);
      try {
        await threadClient.getProperties();
        setChatThreadClient(threadClient);
      } catch (error) {
        console.error('Failed to initialize chat thread client:', error);
      } finally {
        setIsLoading(false);
      }
    };

    initializeChatThreadClient();
  }, [statefulChatClient, threadId]);

  const isDarkMode = useMemo(() => {
    return themeString === 'dark';
  }, [themeString]);

  return (
    <div className={styles.chatScreenContainer}>
      {isLoading || !chatThreadClient ? (
        <LoadingSpinner />
      ) : (
        <>
          <ChatHeader
            personaName={receiverName}
            threadStatus={threadStatus}
            onResolveChat={() => handleOnResolveChat()}
            onShareChat={onShareChat}
            isDark={isDarkMode}
            isResolvingChat={isResolvingChat}
          />
          <div className="flex-1 flex flex-col overflow-hidden">
            <FluentThemeProvider fluentTheme={isDarkMode ? v8DarkTheme : lightTheme} rootStyle={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <ChatClientProvider chatClient={statefulChatClient}>
                <ChatThreadClientProvider chatThreadClient={chatThreadClient}>
                  <ChatComponents 
                    isDarkMode={isDarkMode}
                    threadId={threadId}
                    customerName={receiverName}
                    customerEmail="customer@example.com" 
                    agentEmail={agentEmail || displayName}
                    threadStatus={threadStatus}
                  />
                </ChatThreadClientProvider>
              </ChatClientProvider>
            </FluentThemeProvider>
          </div>
        </>
      )}
    </div>
  );
};
