// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import {
  usePropsFor,
  MessageThread,
  SendBox,
  CustomAvatarOptions,
  ChatMessage,
  CustomMessage,
  Message,
  MessageProps,
  MessageRenderer
} from '@azure/communication-react';
import { useCallback, useEffect, useState } from 'react';
import { strings } from './utils/constants';
import { nanoid } from 'nanoid';
import { useChatComponentsStyles, messageThreadStyles } from './styles/ChatComponents.styles';
import './styles/ChatComponents.css'; // Import CSS overrides for ACS styles
import { MeetingLinkMessage } from './MeetingIntegration';
import { TeamsCallComposite } from './TeamsCallComposite';

interface ChatComponentsProps {
  isResolvedByAgent: boolean;
  isAgentOnline?: boolean;
  onResumeConversation: () => void;
  threadId: string;
  userId: string;
  displayName: string;
}

function ChatComponents(props: ChatComponentsProps): JSX.Element {
  const { isResolvedByAgent, isAgentOnline = false, onResumeConversation, threadId, userId, displayName } = props;
  const styles = useChatComponentsStyles();
  const messageThreadProps = usePropsFor(MessageThread);
  const sendBoxProps = usePropsFor(SendBox);
  const [messages, setMessages] = useState<Message[]>(messageThreadProps?.messages || []);
  
  // Meeting state
  const [showMeetingUI, setShowMeetingUI] = useState(false);
  const [currentMeetingLink, setCurrentMeetingLink] = useState<string | null>(null);
  const [showResolutionPrompt, setShowResolutionPrompt] = useState(false);

  useEffect(() => {
    // merge messageThreadProps.messages with local messages
    if (messageThreadProps?.messages) {
      setMessages((prevMessages) => {
        const serverMessages = messageThreadProps.messages;
        const mergedMessages = [...prevMessages, ...serverMessages];
        // Remove duplicate messages based on messageId
        const uniqueMessages: Message[] = Array.from(
          mergedMessages
            .reduce((map, msg) => {
              if (msg.messageId && msg.messageId !== '' && !map.has(msg.messageId)) {
                map.set(msg.messageId, msg);
              }
              return map;
            }, new Map())
            .values()
        );
        return uniqueMessages.sort((a, b) => a.createdOn.getTime() - b.createdOn.getTime());
      });
    }
  }, [messageThreadProps?.messages]);

  const createSystemMessage = (content: string): CustomMessage => {
    return {
      messageId: nanoid(),
      messageType: 'custom',
      content,
      createdOn: new Date()
    };
  };

  useEffect(() => {
    if (!isResolvedByAgent) {
      return;
    }
    // Insert to the message list
    const resolveSystemMessage = createSystemMessage(strings.conversationResolvedByAgent);
    setMessages((prevMessages) => [...prevMessages, resolveSystemMessage]);
  }, [isResolvedByAgent]);
  const handleOnSendMessage = useCallback(
    async (content: string) => {
      if (isResolvedByAgent) {
        onResumeConversation();
      }
      await sendBoxProps.onSendMessage(content);
    },
    [isResolvedByAgent, onResumeConversation, sendBoxProps]
  );

  // Handle joining a Teams meeting
  const handleJoinMeeting = useCallback((meetingLink: string) => {
    console.log('[ChatComponents] Customer joining meeting:', meetingLink);
    setCurrentMeetingLink(meetingLink);
    setShowMeetingUI(true);
  }, []);

  // Handle when call ends
  const handleCallEnded = useCallback(() => {
    console.log('[ChatComponents] Meeting ended, returning to chat');
    setShowMeetingUI(false);
    setCurrentMeetingLink(null);
    // Show resolution prompt after call ends
    setShowResolutionPrompt(true);
  }, []);

  const onRenderMessage = useCallback(
    (messageProps: MessageProps, messageRenderer?: MessageRenderer) => {
      // Filter out system messages (participant join/leave notifications)
      if (messageProps.message.messageType === 'system') {
        const content = (messageProps.message as any).content || '';
        // Hide participant joined/left messages
        if (content.includes('joined the chat') || content.includes('left the chat')) {
          return <></>; // Return empty fragment instead of null
        }
      }
      
      // Check if this is a chat message with Teams meeting link
      if (messageProps.message.messageType === 'chat') {
        const chatMsg = messageProps.message as ChatMessage;
        const content = chatMsg.content || '';
        // Updated regex to stop at the closing } of the context parameter
        const teamsLinkRegex = /https:\/\/teams\.microsoft\.com\/l\/meetup-join\/[^"\s]+/g;
        
        if (teamsLinkRegex.test(content)) {
          console.log('[ChatComponents] Detected Teams meeting link in message');
          
          return (
            <MeetingLinkMessage
              message={{
                id: chatMsg.messageId || '',
                content: content,
                senderId: chatMsg.senderId || '',
                senderDisplayName: chatMsg.senderDisplayName || 'Unknown',
                createdOn: chatMsg.createdOn
              }}
              threadId={threadId}
              userId={userId}
              onJoinMeeting={handleJoinMeeting}
            />
          );
        }
      }
      
      if (messageProps.message.messageType === 'custom') {
        return (
          <div style={styles.resolveSystemMessageContainer}>
            <div style={styles.resolveSystemMessage}>{messageProps.message.content}</div>
          </div>
        );
      }
      return messageRenderer?.(messageProps) || <></>;
    },
    [styles.resolveSystemMessage, styles.resolveSystemMessageContainer, threadId, userId, handleJoinMeeting]
  );
  const handleOnUpdateMessage = useCallback(
    async (messageId: string, content: string) => {
      const message = messages.find((msg) => msg.messageId === messageId);
      if (message) {
        (message as ChatMessage).content = content;
        setMessages([...messages]);
      }
      await messageThreadProps.onUpdateMessage(messageId, content);
    },
    [messageThreadProps, messages]
  );  
  
  return (
    <div style={styles.container} className="custom-chat-thread">
      {/* Meeting Dialog - Shows Teams-like UI when in meeting */}
      <TeamsCallComposite
        meetingLink={currentMeetingLink || ''}
        threadId={threadId}
        userId={userId}
        displayName={displayName}
        open={showMeetingUI && !!currentMeetingLink}
        onOpenChange={(open: boolean) => {
          if (!open) {
            handleCallEnded();
          }
        }}
      />

      {/* Status Banner - Prominent notification when waiting for agent */}
      {!isAgentOnline && !isResolvedByAgent && (
        <div style={{
          padding: '16px',
          margin: '12px',
          marginBottom: '8px',
          backgroundColor: '#fff7ed',
          border: '2px solid #fb923c',
          borderRadius: '12px',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          boxShadow: '0 4px 12px rgba(251, 146, 60, 0.15)',
          animation: 'slideDown 0.3s ease-out'
        }}>
          <div style={{
            fontSize: '24px',
            animation: 'spin 2s linear infinite'
          }}>
            ⏳
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ 
              fontWeight: 600, 
              color: '#c2410c',
              fontSize: '14px',
              marginBottom: '4px'
            }}>
              Connecting you with a support agent...
            </div>
            <div style={{ 
              fontSize: '13px', 
              color: '#9a3412'
            }}>
              Please wait while we find an available agent to assist you
            </div>
          </div>
        </div>
      )}
      
      <div style={styles.messageThreadContainer}>
        {/*Props are updated asynchronously, so only render the component once props are populated.*/}
        {messageThreadProps && (
          <MessageThread
            {...messageThreadProps}
            messages={messages}
            styles={messageThreadStyles}            
            onRenderAvatar={(userId?: string, options?: CustomAvatarOptions) => {
              const displayName = options?.text || 'User';
              const isAgent = displayName.toLowerCase().includes('agent') || 
                             displayName.toLowerCase().includes('support') ||
                             displayName.toLowerCase().includes('ejaz hussain');
              
              // Generate initials from display name
              const initials = displayName.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
              
              if (isAgent) {
                // Agent Avatar - Soft professional purple/indigo theme
                return (
                  <div className="w-8 h-8 rounded-full flex items-center justify-center text-[11px] font-semibold bg-indigo-500 text-white border border-indigo-400 shadow-sm">
                    {initials}
                  </div>
                );
              } else {
                // Customer Avatar - Soft teal/green theme
                return (
                  <div className="w-8 h-8 rounded-full flex items-center justify-center text-[11px] font-semibold bg-emerald-600 text-white border border-emerald-500 shadow-sm">
                    {initials}
                  </div>
                );
              }
            }}
            onRenderMessage={onRenderMessage}
            onUpdateMessage={handleOnUpdateMessage}
          />
        )}
      </div>
      
      {/* Resolution Prompt - shown after call ends */}
      {showResolutionPrompt && (
        <div className="px-4 py-3 border-t border-gray-200 bg-blue-50">
          <ResolutionPrompt 
            onContinue={() => {
              setShowResolutionPrompt(false);
              handleOnSendMessage("I'd like to continue the conversation");
            }}
            onEndChat={async () => {
              setShowResolutionPrompt(false);
              await handleOnSendMessage("My issue has been resolved. Thank you!");
              // No need to update work item status - just close the prompt
              // The agent can mark it as resolved on their end
            }}
          />
        </div>
      )}

      <div style={styles.sendBoxContainer}>
        <ChatInputBox 
          sendBoxProps={sendBoxProps} 
          onSendMessage={handleOnSendMessage} 
          isResolvedByAgent={isResolvedByAgent} 
        />
      </div>
    </div>
  );
}

/**
 * Modern Send Box Component with sleek design and more typing space
 * Uses Radix UI components for consistent styling
 */
const ChatInputBox = ({ 
  sendBoxProps, 
  onSendMessage, 
  isResolvedByAgent 
}: { 
  sendBoxProps: any; 
  onSendMessage: (content: string) => Promise<void>; 
  isResolvedByAgent: boolean;
}) => {
  const [message, setMessage] = useState('');
  const [isSending, setIsSending] = useState(false);

  const handleSend = async () => {
    if (!message.trim() || isSending) return;
    
    setIsSending(true);
    try {
      await onSendMessage(message.trim());
      setMessage('');
    } catch (error) {
      console.error('Failed to send message:', error);
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div 
      style={{
        padding: '12px 0px', 
        borderTop: '1px solid var(--gray-6)',
        display: 'flex',
        gap: '12px',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
        backgroundColor: 'white',
        width: '100%'
      }}
    >
      <div style={{
        display: 'flex',
        gap: '12px',
        alignItems: 'center',
        maxWidth: '344px',
        width: '100%'        
      }}>
        <input
          type="text"
          placeholder={isResolvedByAgent ? "Chat has been resolved - Read only" : "Type your message..."}
          value={message}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setMessage(e.target.value)}
          onKeyPress={handleKeyPress}
          disabled={isSending || isResolvedByAgent}
          className="flex-1 min-w-0 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:opacity-60 disabled:cursor-not-allowed"
          style={{ opacity: isResolvedByAgent ? 0.6 : 1 }}
        />
        <button 
          onClick={handleSend}
          disabled={!message.trim() || isSending || isResolvedByAgent}
          className="flex-shrink-0 min-w-[40px] px-3 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          style={{ opacity: isResolvedByAgent ? 0.6 : 1 }}
        >
          {isSending ? (
            <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
          ) : (
            <svg width="16" height="16" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M1.20308 1.04312C1.00481 0.954998 0.772341 1.0048 0.627577 1.16641C0.482813 1.32802 0.458794 1.56455 0.568117 1.75196L3.92115 7.50002L0.568117 13.2481C0.458794 13.4355 0.482813 13.672 0.627577 13.8336C0.772341 13.9952 1.00481 14.045 1.20308 13.9569L14.7031 7.95693C14.8836 7.87668 15 7.69762 15 7.50002C15 7.30243 14.8836 7.12337 14.7031 7.04312L1.20308 1.04312ZM4.84553 7.10002L2.21234 2.586L13.2689 7.50002L2.21234 12.414L4.84552 7.90002H9C9.22092 7.90002 9.4 7.72094 9.4 7.50002C9.4 7.27911 9.22092 7.10002 9 7.10002H4.84553Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
            </svg>
          )}
        </button>
      </div>
    </div>
  );
};

/* Add CSS animations */
const animationStyle = document.createElement('style');
animationStyle.textContent = `
  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }
  
  @keyframes slideDown {
    0% {
      opacity: 0;
      transform: translateY(-10px);
    }
    100% {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  @keyframes fadeOut {
    0% {
      opacity: 1;
      transform: translateY(0);
    }
    100% {
      opacity: 0;
      transform: translateY(-20px);
    }
  }
`;
document.head.appendChild(animationStyle);

/**
 * Resolution Prompt Component
 * Shown after a call ends to ask if the issue is resolved
 */
const ResolutionPrompt = ({ 
  onContinue, 
  onEndChat 
}: { 
  onContinue: () => void; 
  onEndChat: () => void;
}) => {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-start gap-2">
        <span className="text-2xl">✓</span>
        <div className="flex-1">
          <h3 className="font-semibold text-gray-800 text-sm mb-1">
            Call Ended
          </h3>
          <p className="text-sm text-gray-600">
            Was your issue resolved during the call?
          </p>
        </div>
      </div>
      
      <div className="flex gap-2">
        <button
          onClick={onEndChat}
          className="flex-1 px-4 py-2.5 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium text-sm transition-colors shadow-sm"
        >
          Yes, End Chat
        </button>
        <button
          onClick={onContinue}
          className="flex-1 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm transition-colors shadow-sm"
        >
          No, Continue Chat
        </button>
      </div>
    </div>
  );
};

export default ChatComponents;
