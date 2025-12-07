import React, { useState } from 'react';
import { ConfigurationScreen } from './ConfigurationScreen';
import { ChatScreen } from './ChatScreen';
import { EndConfirmationScreen } from './EndConfirmationScreen';
import { ErrorScreen } from './ErrorScreen';
import { LoadingSpinner } from './LoadingSpinner';

enum Page {
  Configuration = 'configuration',
  Chat = 'chat',
  EndConfirmation = 'endConfirmation',
  Error = 'error'
}

interface EscalationData {
  conversationId: string;
  customerName: string;
  customerEmail: string;
  messages: any[];
  problemDescription: string;
}

interface ChatFloatingWindowProps {
  onCloseButtonClick: () => void;
  userDisplayName?: string; // Add user display name prop
  userEmail?: string; // Add user email prop
  escalationData?: EscalationData; // Add escalation data to skip form
}

const ChatFloatingWindow: React.FC<ChatFloatingWindowProps> = ({ onCloseButtonClick, userDisplayName, userEmail, escalationData }) => {
  const [page, setPage] = useState(Page.Configuration);
  const [token, setToken] = useState('');
  const [userId, setUserId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [threadId, setThreadId] = useState('');
  const [endpointUrl, setEndpointUrl] = useState('');
  const [questionSummary, setQuestionSummary] = useState('');
  const [agentName, setAgentName] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);
  const [chatThreadClient, setChatThreadClient] = useState<any | undefined>(undefined);

  const renderPage = (): React.ReactElement => {
    switch (page) {
      case Page.Configuration: {
        return (
          <ConfigurationScreen
            onJoinChat={() => {
              setPage(Page.Chat);
            }}
            setToken={setToken}
            setUserId={setUserId}
            setDisplayName={setDisplayName}
            setThreadId={setThreadId}
            setEndpointUrl={setEndpointUrl}
            setAgentName={setAgentName}
            onCloseButtonClicked={onCloseButtonClick}
            onError={(error: string, questionSummary: string) => {
              setQuestionSummary(questionSummary);
              setErrorMessage(error);
              setPage(Page.Error);
            }}
            userDisplayName={escalationData?.customerName || userDisplayName} // Use escalation data first
            userEmail={escalationData?.customerEmail || userEmail} // Use escalation data first
            escalationData={escalationData} // Pass escalation data for auto-submission
          />
        );
      }

      case Page.Chat: {
        if (token && userId && displayName && threadId && endpointUrl) {
          return (
            <ChatScreen
              token={token}
              userId={userId}
              displayName={displayName}
              endpointUrl={endpointUrl}
              threadId={threadId}
              agentName={agentName}
              onEndChat={(chatThreadClient: any) => {
                setChatThreadClient(chatThreadClient);
                // Delete the work item when customer ends chat
                if (threadId) {
                  const acsService = new (require('../services/AzureCommunicationService').AzureCommunicationService)();
                  acsService.deleteWorkItem(threadId).then((deleted: boolean) => {
                    if (deleted) {
                      console.log('[ChatWindow] Work item deleted after customer ended chat');
                    } else {
                      console.log('[ChatWindow] Work item may have already been claimed/deleted');
                    }
                  }).catch((error: any) => {
                    console.error('[ChatWindow] Error deleting work item:', error);
                  });
                }
                setPage(Page.EndConfirmation);
              }}
            />
          );
        }
        return <LoadingSpinner label="Initializing chat..." />;
      }
      case Page.EndConfirmation: {
        return (
          <EndConfirmationScreen
            userId={userId}
            threadId={threadId}
            chatThreadClient={chatThreadClient}
            onConfirmLeaving={() => {
              onCloseButtonClick();
            }}
            onCancel={() => {
              setPage(Page.Chat);
            }}
          />
        );
      }
      default:
        return (
          <ErrorScreen
            message={errorMessage || 'Page not found'}
            onClose={() => {
              setErrorMessage(undefined);
              setQuestionSummary('');
              onCloseButtonClick();
            }}
            onRetrySucceed={() => {
              setQuestionSummary('');
              setPage(Page.Chat);
            }}
            displayName={displayName}
            questionSummary={questionSummary}
            setToken={setToken}
            setUserId={setUserId}
            setThreadId={setThreadId}
            setEndpointUrl={setEndpointUrl}
            setAgentName={setAgentName}
          />
        );
    }
  };

  return (
    <div className="fixed bottom-5 right-5 w-[400px] h-[600px] bg-white rounded-xl shadow-2xl border border-gray-300 z-[1000] flex flex-col">
      {renderPage()}
    </div>
  );
};

export default ChatFloatingWindow;