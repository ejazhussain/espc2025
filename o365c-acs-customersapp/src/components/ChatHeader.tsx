// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';

export interface ChatHeaderProps {
  personaName: string;
  isAgentOnline?: boolean;
  isResolvedByAgent?: boolean;
  onEndChat(): void;
}

export const ChatHeader = (props: ChatHeaderProps): JSX.Element => {
  const { personaName, isAgentOnline = false, isResolvedByAgent = false, onEndChat } = props;  
  
  const getStatusText = () => {
    if (isResolvedByAgent) {
      return 'Chat ended';
    }
    if (isAgentOnline) {
      return 'Agent connected';
    }
    return 'Connecting to agent...';
  };

  const getStatusIcon = () => {
    if (isResolvedByAgent) {
      return null; // No icon when ended
    }
    if (isAgentOnline) {
      return (
        <div className="w-9 h-9 rounded-full bg-white/25 flex items-center justify-center text-lg border-2 border-white/40">
          💬
        </div>
      );
    }
    return (
      <div className="w-9 h-9 rounded-full bg-orange-400/25 flex items-center justify-center text-lg border-2 border-orange-400/40">
        ⏳
      </div>
    );
  };

  const getStatusColor = () => {
    if (isResolvedByAgent) {
      return '#94a3b8'; // gray
    }
    if (isAgentOnline) {
      return '#22c55e'; // green
    }
    return '#f59e0b'; // amber/orange for waiting
  };

  return (
    <div 
      className="flex flex-row items-center justify-between p-4 border-b border-gray-300 bg-gradient-to-br from-primary-600 to-secondary-600 text-white min-h-[64px] overflow-hidden"
      role="banner" 
      aria-label="Chat header"
    >
      <div className="flex items-center gap-3 flex-1 min-w-0 overflow-hidden h-full">
        {/* Status Icon - Modern circular design */}
        {getStatusIcon()}
        
        <div className="flex flex-col gap-1 flex-1 min-w-0 overflow-hidden">
          {/* Title - Modern, lighter weight */}
          <div className="text-base text-white whitespace-nowrap overflow-hidden text-ellipsis font-medium tracking-tight">
            {personaName}
          </div>
          
          {/* Status - Subtle inline design */}
          <div className="flex items-center gap-2">
            {/* Small pulsing dot indicator */}
            {!isResolvedByAgent && (
              <span 
                className={`inline-block w-1.5 h-1.5 rounded-full ${isAgentOnline ? 'animate-pulse' : 'animate-ping'}`}
                style={{
                  backgroundColor: getStatusColor(),
                  boxShadow: `0 0 8px ${getStatusColor()}`
                }}
              />
            )}
            {/* Status text - clean, no badge background */}
            <span className="text-sm text-white/85 whitespace-nowrap font-normal">
              {getStatusText()}
            </span>
          </div>
        </div>
      </div>
      
      <button 
        onClick={onEndChat}
        aria-label="End chat"
        className="rounded p-1 min-w-[24px] h-6 bg-white/10 text-white border border-white/20 cursor-pointer hover:bg-white/20 transition-colors"
      >
        <svg width="12" height="12" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M11.7816 4.03157C12.0062 3.80702 12.0062 3.44295 11.7816 3.2184C11.5571 2.99385 11.193 2.99385 10.9685 3.2184L7.50005 6.68682L4.03164 3.2184C3.80708 2.99385 3.44301 2.99385 3.21846 3.2184C2.99391 3.44295 2.99391 3.80702 3.21846 4.03157L6.68688 7.49999L3.21846 10.9684C2.99391 11.193 2.99391 11.557 3.21846 11.7816C3.44301 12.0061 3.80708 12.0061 4.03164 11.7816L7.50005 8.31316L10.9685 11.7816C11.193 12.0061 11.5571 12.0061 11.7816 11.7816C12.0062 11.557 12.0062 11.193 11.7816 10.9684L8.31322 7.49999L11.7816 4.03157Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
        </svg>
      </button>
    </div>
  );
};
