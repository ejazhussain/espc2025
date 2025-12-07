// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { AIMessage } from '../services/AgentService';
import { cn } from '../theme';

interface MessageBubbleProps {
  message: AIMessage;
  isLastMessage: boolean;
  showEscalation: boolean;
  onSuggestedAction: (action: string) => void;
  onEscalate: () => void;
  isLoading: boolean;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({
  message,
  isLastMessage,
  showEscalation,
  onSuggestedAction,
  onEscalate,
  isLoading
}) => {
  return (
    <div>
      <div 
        className={cn(
          "flex items-start gap-2",
          message.role === 'customer' ? 'justify-end' : 'justify-start'
        )}
      >
        {message.role === 'ai' && <span className="text-xs mt-0.5">🤖</span>}
        
        <div className={cn(
          "max-w-[85%] px-3 py-2 rounded-lg",
          message.role === 'customer' 
            ? 'bg-blue-100 text-blue-900' 
            : 'bg-purple-100 text-gray-900'
        )}>
          {message.role === 'ai' ? (
            <div className="text-sm leading-relaxed text-gray-900">
              <Markdown 
                remarkPlugins={[remarkGfm]}
                components={{
                  h1: ({children, ...props}) => <h1 className="text-lg font-bold my-1" {...props}>{children}</h1>,
                  h2: ({children, ...props}) => <h2 className="text-base font-bold my-1" {...props}>{children}</h2>,
                  h3: ({children, ...props}) => <h3 className="text-sm font-bold my-0.5" {...props}>{children}</h3>,
                  p: ({...props}) => <p className="my-0.5" {...props} />,
                  ul: ({...props}) => <ul className="my-0.5 pl-4 list-disc" {...props} />,
                  ol: ({...props}) => <ol className="my-0.5 pl-4 list-decimal" {...props} />,
                  li: ({...props}) => <li className="my-0.5" {...props} />,
                  code: ({...props}) => <code className="bg-black/10 px-1 py-0.5 rounded text-xs" {...props} />,
                  pre: ({...props}) => <pre className="bg-black/10 p-1.5 rounded overflow-auto text-xs my-1" {...props} />,
                  blockquote: ({...props}) => <blockquote className="border-l-2 border-gray-300 pl-2 my-1 italic" {...props} />
                }}
              >
                {message.content}
              </Markdown>
            </div>
          ) : (
            <div className="text-sm">{message.content}</div>
          )}
          
          {/* Suggested actions and escalation options */}
          {message.role === 'ai' && (
            <div className="mt-2">
              {/* Quick action suggestions */}
              {message.suggestedActions && message.suggestedActions.length > 0 && (
                <>
                  <div className="text-xs text-gray-600 mb-1">Quick actions:</div>
                  <div className="flex gap-1 flex-wrap">
                    {message.suggestedActions.map((action, index) => (
                      <button
                        key={index}
                        className="px-2 py-1 text-xs bg-primary-100 text-primary-700 rounded hover:bg-primary-200 transition-colors"
                        onClick={() => onSuggestedAction(action)}
                      >
                        {action}
                      </button>
                    ))}
                  </div>
                </>
              )}
              
              {/* Escalation option - only on last AI message when triggered */}
              {showEscalation && 
               isLastMessage && 
               message.id !== 'welcome-1' && (
                <div className="mt-1.5 p-1.5 bg-gray-100 rounded">
                  <div className="flex items-center justify-between">
                    <div className="text-xs text-gray-600">
                      {message.recommendEscalation ? '🤝 For complex issues, talk to our specialists' : 'Need more help?'}
                    </div>
                    <button
                      onClick={onEscalate}
                      disabled={isLoading}
                      className="px-2 py-1 text-xs bg-primary-100 text-primary-700 border border-primary-300 rounded hover:bg-primary-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                    >
                      <svg width="12" height="12" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M7.5 0.875C5.49797 0.875 3.875 2.49797 3.875 4.5C3.875 6.15288 4.98124 7.54738 6.49373 7.98351C5.2997 8.12901 4.27557 8.55134 3.50407 9.31167C2.52216 10.2794 2.02502 11.72 2.02502 13.5999C2.02502 13.8623 2.23769 14.0749 2.50002 14.0749C2.76236 14.0749 2.97502 13.8623 2.97502 13.5999C2.97502 11.8799 3.42786 10.7206 4.17091 9.9883C4.91536 9.25463 6.02674 8.87499 7.49995 8.87499C8.97317 8.87499 10.0846 9.25463 10.8291 9.98831C11.5721 10.7206 12.025 11.8799 12.025 13.5999C12.025 13.8623 12.2376 14.0749 12.5 14.0749C12.7623 14.0749 12.975 13.8623 12.975 13.5999C12.975 11.72 12.4778 10.2794 11.4959 9.31167C10.7244 8.55135 9.70025 8.12903 8.50625 7.98352C10.0187 7.5474 11.125 6.15289 11.125 4.5C11.125 2.49797 9.50203 0.875 7.5 0.875ZM4.825 4.5C4.825 3.02264 6.02264 1.825 7.5 1.825C8.97736 1.825 10.175 3.02264 10.175 4.5C10.175 5.97736 8.97736 7.175 7.5 7.175C6.02264 7.175 4.825 5.97736 4.825 4.5Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
                      </svg>
                      Talk to Agent
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
        
        {message.role === 'customer' && <span className="text-xs mt-0.5">👤</span>}
      </div>
    </div>
  );
};
