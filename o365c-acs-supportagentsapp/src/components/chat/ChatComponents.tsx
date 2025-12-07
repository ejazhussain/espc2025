// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { usePropsFor, MessageThread, RichTextSendBox, CustomAvatarOptions } from '@azure/communication-react';
import { messageThreadStyles, useChatComponentsStyles } from '../../styles/ChatComponents.styles';
import { useTheme } from '../../styles/ThemeProvider';
import '../../styles/RichTextSendBox.css';
import { useState } from 'react';
import { MeetingScheduler, MeetingControls } from '../MeetingIntegration';
import { ThreadItemStatus } from './useThreads';

interface ChatComponentsProps {
  isDarkMode: boolean;
  threadId?: string;
  customerName?: string;
  customerEmail?: string;
  agentEmail?: string;
  threadStatus?: ThreadItemStatus;
}

export const ChatComponents = (props: ChatComponentsProps): JSX.Element => {
  const { isDarkMode, threadId, customerName, customerEmail, agentEmail, threadStatus } = props;
  const messageThreadProps = usePropsFor(MessageThread);
  const richTextSendBoxProps = usePropsFor(RichTextSendBox);
  const styles = useChatComponentsStyles();
  const { themeClasses } = useTheme();
  const [activeMeeting, setActiveMeeting] = useState<any>(null);

  const getInitials = (name?: string): string => {
    if (!name) return 'U';
    return name.split(' ').map(word => word.charAt(0)).join('').toUpperCase().substring(0, 2);
  };

  // Check if chat is resolved
  const isResolved = threadStatus === ThreadItemStatus.RESOLVED;

  // Create chat session for meeting integration
  const chatSession = threadId && customerName && customerEmail && agentEmail ? {
    threadId,
    customerId: '', // This would come from thread metadata
    customerName,
    customerEmail,
    agentId: '', // This would come from current user
    agentName: 'Support Agent',
    agentEmail
  } : null;

  return (
    <div className={`h-full ${isDarkMode ? 'bg-gray-900' : 'bg-white'} flex flex-col`}>
      {/* Messages Area */}
      <div className={`flex-1 overflow-auto ${isDarkMode ? 'bg-gray-900' : 'bg-white'}`}>
        {/*Props are updated asynchronously, so only render the component once props are populated.*/}
        {messageThreadProps && (
          <MessageThread
            {...messageThreadProps}
            richTextEditorOptions={{}}
            styles={messageThreadStyles(isDarkMode)}
            onRenderAvatar={(userId?: string, options?: CustomAvatarOptions) => {
              // Determine if this is an agent message based on the display name or user context
              const displayName = options?.text || '';
              const isAgent = displayName.toLowerCase().includes('agent') || 
                             displayName.toLowerCase().includes('support') ||
                             displayName.toLowerCase().includes('system') ||
                             userId?.includes('agent') ||
                             userId?.includes('system');
              
              const initials = isAgent ? 'SA' : getInitials(displayName);
              
              return (
                <div className={`w-9 h-9 rounded-full ${themeClasses.avatar} flex items-center justify-center ${themeClasses.avatarText} font-medium text-sm shadow-md`}>
                  {initials}
                </div>
              );
            }}
          />
        )}
      </div>
      
      {/* Meeting Integration Section - Hidden when chat is resolved */}
      {chatSession && !isResolved && (
        <div className={`flex-shrink-0 ${isDarkMode ? 'bg-gray-800 border-t border-gray-700' : 'bg-white border-t border-gray-200'} p-3`}>
          {activeMeeting ? (
            <MeetingControls
              meeting={activeMeeting}
              onMeetingCancelled={() => setActiveMeeting(null)}
            />
          ) : (
            <MeetingScheduler
              chatSession={chatSession}
              onMeetingCreated={(meeting) => setActiveMeeting(meeting)}
            />
          )}
        </div>
      )}
      
      {/* Send Box Area - Fixed at bottom */}
      <div className={`flex-shrink-0 ${isDarkMode ? 'bg-gray-800 border-t border-gray-700' : 'bg-white border-t border-gray-200'} p-4`}>
        {isResolved ? (
          // Read-only message when chat is resolved
          <div className={`${isDarkMode ? 'bg-gray-700 text-gray-300' : 'bg-gray-100 text-gray-600'} rounded-lg p-4 text-center`}>
            <span className="text-sm font-medium">💬 Chat Resolved - Read Only Mode</span>
          </div>
        ) : (
          richTextSendBoxProps && (
            <div className={`rich-text-send-box-override ${isDarkMode ? 'bg-gray-700' : 'bg-gray-50'} rounded-full shadow-sm transition-all duration-200 hover:shadow-md focus-within:shadow-md ${styles.richTextSendBox}`}>
              <RichTextSendBox {...richTextSendBoxProps} />
            </div>
          )
        )}
      </div>
    </div>
  );
};
