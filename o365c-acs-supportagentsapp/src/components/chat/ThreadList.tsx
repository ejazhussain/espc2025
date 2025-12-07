// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { useCallback, useEffect, useState } from 'react';
import { useTheme } from '../../styles/ThemeProvider';
import { formatTimestampForThread } from '../../utils/datetimeUtils';
import { ThreadItem, ThreadItemStatus } from './useThreads';
import { threadStrings } from '../../constants/constants';
import { LoadingSpinner } from './LoadingSpinner';
import { ThreadListHeader } from './ThreadListHeader';
import { DeleteThreadDialog } from './DeleteThreadDialog';


export interface ThreadListProps {
  threads?: Array<ThreadItem>;
  isLoading: boolean;
  selectedThreadId?: string;
  onThreadSelected(threadId: string): void;
  onDeleteThread?(threadId: string): void;
  tabs: string[];
  selectedTab: string;
  onStatusTabSelected(tabValue: string): void;
  isDark?: boolean;
  incomingQueueCount?: number;
}

export const ThreadList = (props: ThreadListProps): JSX.Element => {
  const { threads, isLoading, selectedThreadId, onThreadSelected, onDeleteThread, tabs, selectedTab, onStatusTabSelected, isDark = true, incomingQueueCount = 0 } = props;
  const { themeClasses } = useTheme();
  
  // State for delete confirmation dialog
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [threadToDelete, setThreadToDelete] = useState<ThreadItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const handleOnThreadSelected = useCallback(
    (threadId: string): void => {
      onThreadSelected(threadId);
    },
    [onThreadSelected]
  );

  const handleDeleteClick = (thread: ThreadItem) => {
    setThreadToDelete(thread);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (threadToDelete && onDeleteThread) {
      setIsDeleting(true);
      try {
        await onDeleteThread(threadToDelete.id);
        setDeleteDialogOpen(false);
        setThreadToDelete(null);
      } catch (error) {
        console.error('Failed to delete thread:', error);
        // Error notification is now handled by the ThreadService/useThreads hook
        setDeleteDialogOpen(false);
        setThreadToDelete(null);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  const handleDeleteCancel = () => {
    if (!isDeleting) { // Prevent cancellation while deleting
      setDeleteDialogOpen(false);
      setThreadToDelete(null);
      setIsDeleting(false);
    }
  };

  // Select the first thread when the component is mounted
  useEffect(() => {
    if (!selectedThreadId && threads && threads?.length > 0) {
      // Convert tab name to status value
      const statusValue = selectedTab.toLowerCase() === 'active' ? ThreadItemStatus.ACTIVE : ThreadItemStatus.RESOLVED;
      const firstThread = threads.find((thread) => thread.status === statusValue);
      firstThread && handleOnThreadSelected(firstThread?.id);
    }
  }, [handleOnThreadSelected, selectedTab, selectedThreadId, threads]);

  const getInitials = (name: string): string => {
    return name.split(' ').map(word => word.charAt(0)).join('').toUpperCase().substring(0, 2);
  };

  const threadItem = (thread: ThreadItem): JSX.Element => {
    const isSelected = thread.id === selectedThreadId;
    
    return (
      <div 
        key={thread.id} 
        className={`
          group flex items-center p-4 rounded-xl cursor-pointer transition-all duration-200
          ${isSelected 
            ? isDark 
              ? `bg-gray-800 border-l-3 ${themeClasses.selectedBorder} shadow-sm` 
              : `${themeClasses.selectedBgLight} border-l-3 ${themeClasses.selectedBorder} shadow-sm`
            : isDark 
              ? 'bg-transparent hover:bg-gray-800/50 transition-colors'
              : 'bg-transparent hover:bg-gray-50/80 transition-colors'
          }
        `}
        onClick={() => handleOnThreadSelected(thread.id)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            handleOnThreadSelected(thread.id);
          }
        }}
      >
        {/* Avatar with online indicator */}
        <div className="relative mr-4">
          <div className={`w-11 h-11 rounded-full ${themeClasses.avatar} flex items-center justify-center ${themeClasses.avatarText} font-normal text-base shadow-sm`}>
            {getInitials(thread.topic)}
          </div>
          {thread.status === ThreadItemStatus.ACTIVE && (
            <div className={`absolute -bottom-0.5 -right-0.5 w-3.5 h-3.5 ${themeClasses.statusDot} rounded-full border-2 ${isDark ? 'border-gray-900' : 'border-white'}`}></div>
          )}
        </div>

        {/* Thread content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between">
            <h3 className={`${isDark ? 'text-white' : 'text-gray-800'} font-medium text-sm truncate pr-2`}>
              {thread.topic}
            </h3>
            <div className="flex items-center space-x-2">
              <span className={`${isDark ? 'text-gray-400' : 'text-gray-500'} text-xs whitespace-nowrap`}>
                {formatTimestampForThread(thread.lastMessageReceivedOn, new Date(), threadStrings)}
              </span>
              {onDeleteThread && (
                <button
                  onClick={(e) => {
                    e.stopPropagation(); // Prevent thread selection when clicking delete
                    handleDeleteClick(thread);
                  }}
                  className={`
                    opacity-0 group-hover:opacity-100 transition-all duration-200 transform hover:scale-110
                    w-8 h-8 rounded-lg flex items-center justify-center
                    ${isDark 
                      ? 'hover:bg-red-900/30 text-gray-400 hover:text-red-400' 
                      : 'hover:bg-red-50 text-gray-400 hover:text-red-500'
                    }
                    focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-1
                  `}
                  title="Delete conversation"
                  aria-label={`Delete conversation ${thread.topic}`}
                >
                  {/* Modern Recycle Bin Icon */}
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              )}
            </div>
          </div>
          
          {/* Last message placeholder */}
          <p className={`${isDark ? 'text-gray-500' : 'text-gray-500'} text-xs truncate mt-1`}>
            Last activity: {formatTimestampForThread(thread.lastMessageReceivedOn, new Date(), threadStrings)}
          </p>
          
          {thread.status === ThreadItemStatus.ACTIVE && (
            <div className="flex items-center mt-2">
              <div className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${
                isDark 
                  ? 'bg-green-900/50 text-green-300 border-green-700' 
                  : 'bg-green-100 text-green-800 border-green-200'
              }`}>
                <div className={`w-1 h-1 rounded-full mr-1 ${
                  isDark ? 'bg-green-400' : 'bg-green-500'
                }`}></div>
                Active
              </div>
            </div>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className={`h-full ${isDark ? 'bg-gray-900' : 'bg-gray-50'} flex flex-col`}>
      {/* Header with tabs */}
      <div className={`${isDark ? 'border-b border-gray-700' : 'border-b border-gray-100'} p-6`}>
        <ThreadListHeader
          tabs={tabs}
          selectedTab={selectedTab}
          onStatusTabSelected={onStatusTabSelected}
          isDark={isDark}
          incomingQueueCount={incomingQueueCount}
        />
      </div>

      {/* Thread list */}
      <div className="flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center justify-center h-32">
            <LoadingSpinner />
          </div>
        ) : (
          <div className="p-3 space-y-1">
            {(() => {
              // Filter threads by selected tab status
              const statusValue = selectedTab.toLowerCase() === 'active' ? ThreadItemStatus.ACTIVE : ThreadItemStatus.RESOLVED;
              const filteredThreads = threads?.filter((thread) => thread.status === statusValue) || [];
              
              if (filteredThreads.length > 0) {
                // Show filtered threads
                return filteredThreads.map((thread) => threadItem(thread));
              } else {
                // Show empty state message
                return (
                  <div className="flex flex-col items-center justify-center h-32 px-4">
                    <div className={`w-12 h-12 rounded-full ${isDark ? 'bg-gray-800' : 'bg-gray-200'} flex items-center justify-center mb-3`}>
                      <svg className={`w-6 h-6 ${isDark ? 'text-gray-500' : 'text-gray-400'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                      </svg>
                    </div>
                    <p className={`${isDark ? 'text-gray-400' : 'text-gray-500'} text-sm text-center mb-1 font-medium`}>
                      {selectedTab.toLowerCase() === 'active' 
                        ? 'No active conversations'
                        : 'No resolved conversations'
                      }
                    </p>
                    <p className={`${isDark ? 'text-gray-500' : 'text-gray-400'} text-xs text-center`}>
                      {selectedTab.toLowerCase() === 'active' 
                        ? 'No conversations are currently active.'
                        : 'No conversations have been resolved yet.'
                      }
                    </p>
                  </div>
                );
              }
            })()}
          </div>
        )}
      </div>

      {/* Delete Confirmation Dialog */}
      <DeleteThreadDialog
        isOpen={deleteDialogOpen}
        threadTopic={threadToDelete?.topic || ''}
        onConfirm={handleDeleteConfirm}
        onCancel={handleDeleteCancel}
        isDark={isDark}
        isDeleting={isDeleting}
      />
    </div>
  );
};
