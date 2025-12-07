// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ThreadItemStatus } from './useThreads';
import { useTheme } from '../../styles/ThemeProvider';

export interface ChatHeaderProps {
  personaName: string;
  threadStatus: ThreadItemStatus;
  onResolveChat(): void;
  onShareChat?: () => void;
  isDark?: boolean;
  isResolvingChat?: boolean;
}

export const ChatHeader = (props: ChatHeaderProps): JSX.Element => {
  const { personaName, threadStatus, onResolveChat, onShareChat, isDark = true, isResolvingChat = false } = props;
  const { themeClasses } = useTheme();
  
  const getInitials = (name: string): string => {
    // Extract just the person's name from formatted title like "Alan Partridge - Training Support (Today 12:51)"
    const personName = name.split(' - ')[0];
    return personName.split(' ').map(word => word.charAt(0)).join('').toUpperCase().substring(0, 2);
  };

  return (
    <div className={`${isDark ? 'bg-gray-800 border-b border-gray-700' : 'bg-gray-50 border-b border-gray-200'} p-4 flex items-center justify-between`}>
      {/* User Info */}
      <div className="flex items-center space-x-4">
        <div className="relative">
          <div className={`w-11 h-11 rounded-full ${themeClasses.avatar} flex items-center justify-center text-white font-normal text-base shadow-sm`}>
            {getInitials(personaName)}
          </div>
          {threadStatus === ThreadItemStatus.ACTIVE && (
            <div className={`absolute -bottom-0.5 -right-0.5 w-3.5 h-3.5 ${themeClasses.statusDot} rounded-full border-2 ${isDark ? 'border-gray-800' : 'border-white'}`}></div>
          )}
        </div>
        
        <div>
          <h1 className={`text-lg font-medium ${isDark ? 'text-white' : 'text-gray-800'}`}>
            {personaName}
          </h1>
          <div className="flex items-center space-x-2 mt-1">
            {threadStatus === ThreadItemStatus.ACTIVE ? (
              <div className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 border border-green-200">
                <div className="w-1.5 h-1.5 bg-green-500 rounded-full mr-1.5"></div>
                ACTIVE CHAT
              </div>
            ) : (
              <div className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600 border border-gray-200">
                <div className="w-1.5 h-1.5 bg-gray-500 rounded-full mr-1.5"></div>
                RESOLVED
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex items-center space-x-3">
        {/* Share Chat Button - Show only for resolved chats */}
        {threadStatus === ThreadItemStatus.RESOLVED && onShareChat && (
          <button
            onClick={onShareChat}
            className={`
              ${themeClasses.button} text-white px-5 py-2 rounded-lg
              font-normal text-sm transition-all duration-200 shadow-sm
              hover:shadow-md flex items-center space-x-2
            `}
          >
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path d="M15 8a3 3 0 10-2.977-2.63l-4.94 2.47a3 3 0 100 4.319l4.94 2.47a3 3 0 10.895-1.789l-4.94-2.47a3.027 3.027 0 000-.74l4.94-2.47C13.456 7.68 14.19 8 15 8z" />
            </svg>
            <span>Share Chat</span>
          </button>
        )}

        {/* Resolve Button - Show only for active chats */}
        {threadStatus === ThreadItemStatus.ACTIVE && (
          <button
            onClick={onResolveChat}
            disabled={isResolvingChat}
            className={`
              ${themeClasses.button} text-white px-5 py-2 rounded-lg
              font-normal text-sm transition-all duration-200 shadow-sm
              hover:shadow-md flex items-center space-x-2
              ${isResolvingChat ? 'opacity-50 cursor-not-allowed' : 'hover:shadow-md'}
            `}
          >
            {isResolvingChat ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>Resolving...</span>
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
                <span>Resolve</span>
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
};

export default ChatHeader;


