import React from 'react';

interface EndConfirmationScreenProps {
  userId: string;
  threadId: string;
  chatThreadClient: any;
  onConfirmLeaving: () => void;
  onCancel: () => void;
}

export const EndConfirmationScreen: React.FC<EndConfirmationScreenProps> = ({
  userId,
  threadId,
  chatThreadClient,
  onConfirmLeaving,
  onCancel
}) => {
  return (
    <div className="p-5 h-full flex flex-col justify-center items-center">
      <div className="text-center mb-8">
        <div className="text-4xl mb-3">
          🤔
        </div>
        <h2 className="text-xl font-bold mb-2">
          End Chat Session?
        </h2>
        <p className="text-base text-gray-600">
          Are you sure you want to leave this chat? This action cannot be undone.
        </p>
      </div>

      <div className="flex flex-col gap-3 w-full">
        <button
          onClick={onConfirmLeaving}
          className="px-4 py-3 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
        >
          Yes, End Chat
        </button>
        <button
          onClick={onCancel}
          className="px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Continue Chat
        </button>
      </div>

      <p className="text-xs text-gray-500 mt-5 text-center">
        Chat ID: {threadId.slice(-8)}
      </p>
    </div>
  );
};