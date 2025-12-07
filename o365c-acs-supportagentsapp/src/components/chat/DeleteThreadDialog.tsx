// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import React from 'react';

export interface DeleteThreadDialogProps {
  isOpen: boolean;
  threadTopic: string;
  onConfirm: () => void;
  onCancel: () => void;
  isDark?: boolean;
  isDeleting?: boolean;
}

export const DeleteThreadDialog: React.FC<DeleteThreadDialogProps> = ({
  isOpen,
  threadTopic,
  onConfirm,
  onCancel,
  isDark = true,
  isDeleting = false
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div 
        className={`absolute inset-0 bg-black bg-opacity-50 backdrop-blur-sm transition-opacity ${
          isDeleting ? 'cursor-not-allowed' : 'cursor-pointer'
        }`}
        onClick={isDeleting ? undefined : onCancel}
      />
      
      {/* Dialog */}
      <div className={`
        relative w-full max-w-md rounded-2xl shadow-2xl border transition-all transform
        ${isDark 
          ? 'bg-gray-800 border-gray-700' 
          : 'bg-white border-gray-200'
        }
      `}>
        {/* Header */}
        <div className="p-6 pb-4">
          <div className="flex items-center space-x-3">
            {/* Warning Icon */}
            <div className={`
              flex-shrink-0 w-12 h-12 rounded-full flex items-center justify-center
              ${isDark 
                ? 'bg-red-900/30 text-red-400' 
                : 'bg-red-100 text-red-600'
              }
            `}>
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            
            <div>
              <h3 className={`text-lg font-semibold ${isDark ? 'text-white' : 'text-gray-900'}`}>
                Delete Conversation
              </h3>
              <p className={`text-sm mt-1 ${isDark ? 'text-gray-400' : 'text-gray-600'}`}>
                This action cannot be undone
              </p>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="px-6 pb-6">
          <p className={`text-sm ${isDark ? 'text-gray-300' : 'text-gray-700'}`}>
            Are you sure you want to delete the conversation with{' '}
            <span className="font-medium">{threadTopic}</span>? 
            All messages and history will be permanently removed.
          </p>
        </div>

        {/* Actions */}
        <div className={`
          flex space-x-3 px-6 py-4 rounded-b-2xl
          ${isDark ? 'bg-gray-750' : 'bg-gray-50'}
        `}>
          <button
            onClick={onCancel}
            disabled={isDeleting}
            className={`
              flex-1 px-4 py-2.5 rounded-xl font-medium transition-all duration-200
              ${isDeleting
                ? 'bg-gray-600 text-gray-400 cursor-not-allowed'
                : isDark 
                  ? 'bg-gray-700 text-gray-300 hover:bg-gray-600 hover:text-white border border-gray-600' 
                  : 'bg-white text-gray-700 hover:bg-gray-100 border border-gray-300'
              }
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
            `}
          >
            Cancel
          </button>
          
          <button
            onClick={onConfirm}
            disabled={isDeleting}
            className={`
              flex-1 px-4 py-2.5 rounded-xl font-medium transition-all duration-200 flex items-center justify-center
              ${isDeleting
                ? 'bg-red-500 cursor-not-allowed'
                : isDark
                  ? 'bg-red-600 hover:bg-red-700 shadow-lg hover:shadow-red-500/25'
                  : 'bg-red-600 hover:bg-red-700 shadow-lg hover:shadow-red-500/25'
              }
              text-white focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2
            `}
          >
            {isDeleting ? (
              <>
                {/* Loading Spinner */}
                <svg
                  className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  ></circle>
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  ></path>
                </svg>
                Deleting...
              </>
            ) : (
              'Delete'
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
