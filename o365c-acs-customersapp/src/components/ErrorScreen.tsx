import React from 'react';

interface ErrorScreenProps {
  message: string;
  onClose: () => void;
  onRetrySucceed: () => void;
  displayName: string;
  questionSummary: string;
  setToken: (token: string) => void;
  setUserId: (userId: string) => void;
  setThreadId: (threadId: string) => void;
  setEndpointUrl: (endpointUrl: string) => void;
  setAgentName: (agentName: string) => void;
}

export const ErrorScreen: React.FC<ErrorScreenProps> = ({
  message,
  onClose,
  onRetrySucceed,
  displayName,
  questionSummary,
  setToken,
  setUserId,
  setThreadId,
  setEndpointUrl,
  setAgentName
}) => {
  const [isRetrying, setIsRetrying] = React.useState(false);

  const handleRetry = () => {
    setIsRetrying(true);
    
    // Mock retry logic - replace with actual ACS retry
    setTimeout(() => {
      setToken('retry-token-' + Date.now());
      setUserId('retry-user-' + Date.now());
      setThreadId('retry-thread-' + Date.now());
      setEndpointUrl('https://retry-endpoint.communication.azure.com');
      setAgentName('AI Support Agent');
      setIsRetrying(false);
      onRetrySucceed();
    }, 2000);
  };

  return (
    <div className="p-5 h-full flex flex-col justify-center items-center">
      <div className="text-center mb-8">
        <div className="text-4xl mb-3">
          ⚠️
        </div>
        <h2 className="text-xl font-bold mb-2">
          Connection Error
        </h2>
        <p className="text-base text-gray-600 mb-4">
          {message}
        </p>
        {displayName && questionSummary && (
          <div className="bg-gray-50 p-3 rounded-lg">
            <p className="text-sm text-gray-600">
              <strong>Name:</strong> {displayName}
            </p>
            <p className="text-sm text-gray-600 mt-1">
              <strong>Question:</strong> {questionSummary}
            </p>
          </div>
        )}
      </div>

      <div className="flex flex-col gap-3 w-full">
        <button
          onClick={handleRetry}
          disabled={isRetrying}
          className="px-4 py-3 bg-gradient-to-br from-primary-600 to-secondary-600 text-white rounded-lg hover:opacity-90 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isRetrying ? 'Retrying...' : 'Try Again'}
        </button>
        <button
          onClick={onClose}
          className="px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Close
        </button>
      </div>
    </div>
  );
};