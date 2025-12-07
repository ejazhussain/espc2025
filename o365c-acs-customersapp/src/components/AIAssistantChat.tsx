// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useState, useRef, useEffect } from 'react';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { AgentService, AIMessage } from '../services/AgentService';
import { cn } from '../theme';

// Use AIMessage from service

interface EscalationData {
  conversationId: string;
  customerName: string;
  customerEmail: string;
  messages: AIMessage[];
  problemDescription: string;
}

interface AIAssistantProps {
  onEscalateToAgent?: (conversationId: string, messages: AIMessage[], escalationData?: EscalationData) => void;
  onClose?: () => void;
  customerName?: string;
  customerEmail?: string;
  initialQuestion?: string;
}

export const AIAssistantChat: React.FC<AIAssistantProps> = ({
  onEscalateToAgent,
  onClose,
  customerName = 'Customer',
  customerEmail = '',
  initialQuestion = ''
}) => {
  const [messages, setMessages] = useState<AIMessage[]>([]);
  const [currentMessage, setCurrentMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showEscalation, setShowEscalation] = useState(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  // Lazy initialization - only creates service when first message is sent
  const agentServiceRef = useRef<AgentService>();

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Initialize with welcome message
  useEffect(() => {
    const welcomeMessage: AIMessage = {
      id: 'welcome-1',
      role: 'ai',
      content: `Hello ${customerName}! I'm here to help you with any IT support questions or issues you might have. What can I help you with today?`,
      timestamp: new Date(),
      confidenceScore: 1.0,
      recommendEscalation: false,
      suggestedActions: ['Password reset help', 'Outlook not working', 'SharePoint access', 'Teams issues', 'Account locked']
    };
    setMessages([welcomeMessage]);
    // Ensure escalation is hidden initially
    setShowEscalation(false);
  }, [customerName]);

  const handleSendMessage = async () => {
    if (!currentMessage.trim() || isLoading) return;

    // Enhanced detection for user requesting human help
    const humanRequestKeywords = [
      'human', 'agent', 'person', 'someone', 'specialist', 'representative',
      'talk to', 'speak to', 'connect me', 'transfer me', 'escalate',
      'human help', 'real person', 'customer service', 'support agent',
      'not helpful', 'doesn\'t work', 'still need help', 'can\'t solve'
    ];
    
    const messageText = currentMessage.trim().toLowerCase();
    const isRequestingHuman = humanRequestKeywords.some(keyword => 
      messageText.includes(keyword)
    );
    
    // If user is explicitly requesting human help, don't send to AI at all
    if (isRequestingHuman) {
      setShowEscalation(true);
      
      // Add user message as-is (don't modify it)
      const userMessage: AIMessage = {
        id: `msg-${Date.now()}`,
        role: 'customer',
        content: currentMessage.trim(),
        timestamp: new Date()
      };
      
      setMessages(prev => [...prev, userMessage]);
      setCurrentMessage('');
      
      // Add AI response acknowledging the escalation request (UI feedback only)
      const aiResponse: AIMessage = {
        id: `ai-${Date.now()}`,
        role: 'ai',
        content: "I understand you'd like to speak with a human agent. I'll connect you with one of our specialists who can provide personalized assistance.",
        timestamp: new Date(),
        confidenceScore: 1.0,
        recommendEscalation: true,
        escalationReason: 'User requested human assistance',
        isSystemMessage: true // Mark as system message - won't be sent to human agent
      };
      
      setMessages(prev => [...prev, aiResponse]);
      return; // Don't continue with AI processing
    }

    // For non-escalation messages, clean them and process with AI
    let cleanedMessage = currentMessage.trim();
    
    // Only clean escalation phrases when they're mixed with actual content
    // e.g., "Account locked | speak to agent" -> "Account locked"
    const escalationPhrases = [
      /\s*\|\s*speak to agent\s*/gi,
      /\s*\|\s*talk to agent\s*/gi,
      /\s*\|\s*connect me with agent\s*/gi,
      /\s*\|\s*transfer me to agent\s*/gi
    ];
    
    escalationPhrases.forEach(phrase => {
      cleanedMessage = cleanedMessage.replace(phrase, '').trim();
    });

    const userMessage: AIMessage = {
      id: `msg-${Date.now()}`,
      role: 'customer',
      content: cleanedMessage,
      timestamp: new Date()
    };

    // Add user message immediately
    setMessages(prev => [...prev, userMessage]);
    setCurrentMessage('');
    setIsLoading(true);

    try {
      // Lazy initialization - create service only on first use
      if (!agentServiceRef.current) {
        agentServiceRef.current = new AgentService();
      }

      // Call Agent API using the simplified service with cleaned message
      const agentResponse = await agentServiceRef.current.sendMessage(cleanedMessage);

      if (!agentResponse.success) {
        throw new Error(agentResponse.errorMessage || 'Agent processing failed');
      }

      const aiMessage: AIMessage = {
        id: `ai-${Date.now()}`,
        role: 'ai',
        content: agentResponse.response,
        timestamp: new Date(agentResponse.timestamp),
        confidenceScore: agentResponse.confidenceScore,
        // Only recommend escalation for very low confidence or explicit AI suggestion
        recommendEscalation: agentResponse.confidenceScore < 0.5,
        escalationReason: agentResponse.confidenceScore < 0.5 ? 'Low confidence response' : undefined,
        suggestedActions: []
      };

      setMessages(prev => [...prev, aiMessage]);
      
      // Only show escalation if AI explicitly recommends it (very low confidence)
      if (aiMessage.recommendEscalation) {
        setShowEscalation(true);
      }

    } catch (error) {
      console.error('Error sending message to AI assistant:', error);
      
      // Add error message
      const errorMessage: AIMessage = {
        id: `error-${Date.now()}`,
        role: 'ai',
        content: "I'm sorry, I'm having trouble processing your request right now. Would you like me to connect you with a human support agent?",
        timestamp: new Date(),
        recommendEscalation: true,
        escalationReason: 'AI service unavailable'
      };
      
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleEscalateToAgent = () => {
    console.log('🔄 Escalating to human agent...');
    
    // Add a "connecting" message from AI ONLY for UI feedback (won't be sent to agent)
    const connectingMessage: AIMessage = {
      id: `connecting-${Date.now()}`,
      role: 'ai',
      content: "Perfect! I'm connecting you with one of our IT support agents now. They have your conversation history and will be able to provide personalized assistance.",
      timestamp: new Date(),
      isSystemMessage: true // Mark as system message to exclude from history
    };
    
    setMessages(prev => [...prev, connectingMessage]);
    
    // Call parent callback to switch to human agent flow with user info
    if (onEscalateToAgent) {
      // Filter out system messages (escalation acknowledgments) from conversation history
      // Only send actual customer questions and AI responses, not UI feedback messages
      // Also filter out the escalation request message itself ("I need to speak with agent")
      const conversationHistoryForAgent = messages.filter(msg => {
        // Exclude system messages
        if (msg.isSystemMessage) return false;
        
        // Exclude welcome message
        if (msg.id === 'welcome-1') return false;
        
        // Exclude AI messages that recommend escalation
        if (msg.recommendEscalation && msg.role === 'ai') return false;
        
        // Exclude customer messages that are ONLY escalation requests
        if (msg.role === 'customer') {
          const content = msg.content.toLowerCase().trim();
          const escalationOnlyPhrases = [
            'i need to speak with agent',
            'i need to speak to agent',
            'i want to speak with agent',
            'i want to speak to agent',
            'speak to agent',
            'talk to agent',
            'speak with agent',
            'talk with agent',
            'connect me with agent',
            'connect me to agent',
            'transfer me to agent',
            'escalate to human',
            'need human help',
            'i want to talk to someone',
            'connect me to a person'
          ];
          
          // If the entire message is just an escalation request, exclude it
          if (escalationOnlyPhrases.includes(content)) {
            return false;
          }
        }
        
        return true;
      });
      
      console.log('[AI Assistant] Filtered conversation history:', {
        originalCount: messages.length,
        filteredCount: conversationHistoryForAgent.length,
        excluded: messages.length - conversationHistoryForAgent.length
      });
      
      // Create escalation data with customer info and FILTERED conversation context
      const escalationData: EscalationData = {
        conversationId: `escalation-${Date.now()}`,
        customerName: customerName,
        customerEmail: customerEmail,
        messages: conversationHistoryForAgent, // Send filtered history without system messages
        // Extract the main issue from recent customer messages
        problemDescription: extractProblemDescription(conversationHistoryForAgent)
      };
      
      onEscalateToAgent(escalationData.conversationId, escalationData.messages, escalationData);
    }
  };

  // Helper function to extract the customer's main problem from conversation
  const extractProblemDescription = (msgs: AIMessage[]): string => {
    const customerMessages = msgs.filter(m => m.role === 'customer').map(m => {
      let cleanContent = m.content;
      
      // Clean escalation phrases from customer messages
      const escalationPhrases = [
        /\s*\|\s*speak to agent\s*/gi,
        /\s*\|\s*talk to agent\s*/gi,
        /\s*speak to agent\s*/gi,
        /\s*talk to agent\s*/gi,
        /\s*connect me with agent\s*/gi,
        /\s*transfer me to agent\s*/gi,
        /\s*escalate to human\s*/gi,
        /\s*need human help\s*/gi,
        /\s*I want to talk to someone\s*/gi,
        /\s*connect me to a person\s*/gi
      ];
      
      escalationPhrases.forEach(phrase => {
        cleanContent = cleanContent.replace(phrase, '').trim();
      });
      
      return cleanContent;
    }).filter(content => content.length > 0); // Remove empty messages
    
    if (customerMessages.length > 0) {
      // Return the most recent meaningful message instead of joining with pipes
      return customerMessages[customerMessages.length - 1];
    }
    return 'Customer needs IT support assistance';
  };

  const handleKeyPress = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      handleSendMessage();
    }
  };

  const handleSuggestedAction = (action: string) => {
    setCurrentMessage(action);
  };

  return (
    <div className="h-[600px] flex flex-col bg-gradient-to-br from-primary-600 to-secondary-600 border border-gray-300 rounded-xl shadow-2xl overflow-hidden">
      {/* Header - Modern design matching ChatHeader */}
      <div className="p-4 border-b border-gray-300 bg-gradient-to-br from-primary-600 to-secondary-600 text-white min-h-[64px] overflow-hidden">
        <div className="flex items-center gap-3 h-full">
          {/* Modern circular icon badge */}
          <div className="w-9 h-9 rounded-full bg-white/25 flex items-center justify-center text-lg border-2 border-white/40 flex-shrink-0">
            🤖
          </div>
          <div className="flex flex-col gap-1 flex-1 min-w-0 overflow-hidden">
            {/* Modern typography - lighter weight */}
            <div className="text-base text-white whitespace-nowrap overflow-hidden text-ellipsis font-medium tracking-tight">
              AI Support Assistant
            </div>
            {/* Subtle subtitle */}
            <div className="text-xs text-white/85 whitespace-nowrap overflow-hidden text-ellipsis font-normal">
              Instant help for your questions
            </div>
          </div>
          {onClose && (
            <button
              onClick={onClose}
              aria-label="Close AI chat"
              className="rounded p-1 min-w-[24px] h-6 bg-white/10 text-white border border-white/20 flex-shrink-0 cursor-pointer hover:bg-white/20 transition-colors"
            >
              <svg width="12" height="12" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M11.7816 4.03157C12.0062 3.80702 12.0062 3.44295 11.7816 3.2184C11.5571 2.99385 11.193 2.99385 10.9685 3.2184L7.50005 6.68682L4.03164 3.2184C3.80708 2.99385 3.44301 2.99385 3.21846 3.2184C2.99391 3.44295 2.99391 3.80702 3.21846 4.03157L6.68688 7.49999L3.21846 10.9684C2.99391 11.193 2.99391 11.557 3.21846 11.7816C3.44301 12.0061 3.80708 12.0061 4.03164 11.7816L7.50005 8.31316L10.9685 11.7816C11.193 12.0061 11.5571 12.0061 11.7816 11.7816C12.0062 11.557 12.0062 11.193 11.7816 10.9684L8.31322 7.49999L11.7816 4.03157Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Messages Area - White background with margin like agent chat */}
      <div className="flex-1 bg-white m-2 rounded-lg relative overflow-hidden flex flex-col">
        <div className="flex-1 overflow-y-auto p-3 flex flex-col gap-2">
        {messages.map((message) => (
          <div key={message.id}>
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
                  <div style={{ 
                    fontSize: '14px', 
                    lineHeight: '1.5',
                    color: 'var(--gray-12)'
                  }}>
                    <Markdown 
                      remarkPlugins={[remarkGfm]}
                      components={{
                        // Style headings
                        h1: ({children, ...props}) => <h1 style={{fontSize: '18px', fontWeight: 'bold', margin: '4px 0 2px 0'}} {...props}>{children}</h1>,
                        h2: ({children, ...props}) => <h2 style={{fontSize: '16px', fontWeight: 'bold', margin: '3px 0 2px 0'}} {...props}>{children}</h2>,
                        h3: ({children, ...props}) => <h3 style={{fontSize: '14px', fontWeight: 'bold', margin: '2px 0 1px 0'}} {...props}>{children}</h3>,
                        // Style paragraphs
                        p: ({...props}) => <p style={{margin: '2px 0'}} {...props} />,
                        // Style lists
                        ul: ({...props}) => <ul style={{margin: '2px 0', paddingLeft: '16px'}} {...props} />,
                        ol: ({...props}) => <ol style={{margin: '2px 0', paddingLeft: '16px'}} {...props} />,
                        li: ({...props}) => <li style={{margin: '1px 0'}} {...props} />,
                        // Style code
                        code: ({...props}) => <code style={{backgroundColor: 'rgba(0,0,0,0.1)', padding: '1px 3px', borderRadius: '3px', fontSize: '13px'}} {...props} />,
                        pre: ({...props}) => <pre style={{backgroundColor: 'rgba(0,0,0,0.1)', padding: '6px', borderRadius: '4px', overflow: 'auto', fontSize: '13px'}} {...props} />,
                        // Style blockquotes
                        blockquote: ({...props}) => <blockquote style={{borderLeft: '3px solid #ddd', paddingLeft: '8px', margin: '4px 0', fontStyle: 'italic'}} {...props} />
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
                        <div className="flex flex-wrap gap-1">
                          {message.suggestedActions.map((action, index) => (
                            <button
                              key={index}
                              onClick={() => handleSuggestedAction(action)}
                              className="px-2 py-1 text-xs bg-primary-100 text-primary-700 rounded hover:bg-primary-200 transition-colors"
                            >
                              {action}
                            </button>
                          ))}
                        </div>
                      </>
                    )}
                    
                    {/* Conditionally show escalation option ONLY on the last AI message AND when explicitly triggered */}
                    {showEscalation && 
                     message.id === messages[messages.length - 1]?.id && 
                     message.id !== 'welcome-1' && (
                      <div className="mt-2 p-2 bg-gray-50 rounded-lg">
                        <div className="flex items-center justify-between">
                          <span className="text-xs text-gray-600">
                            {message.recommendEscalation ? '🤝 For complex issues, talk to our specialists' : 'Need more help?'}
                          </span>
                          <button
                            onClick={handleEscalateToAgent}
                            disabled={isLoading}
                            className="px-2 py-1 text-xs bg-primary-100 text-primary-700 rounded hover:bg-primary-200 transition-colors border border-primary-300 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                          >
                            <svg width="12" height="12" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
                              <path d="M7.5 0.875C5.49797 0.875 3.875 2.49797 3.875 4.5C3.875 6.15288 4.98124 7.54738 6.49373 7.98351C5.2997 8.12901 4.27557 8.55134 3.50407 9.31167C2.52216 10.2794 2.02502 11.72 2.02502 13.5999C2.02502 13.8623 2.23769 14.0749 2.50002 14.0749C2.76236 14.0749 2.97502 13.8623 2.97502 13.5999C2.97502 11.8799 3.42786 10.7206 4.17091 9.9883C4.91536 9.25463 6.02674 8.87499 7.49995 8.87499C8.97317 8.87499 10.0846 9.25463 10.8291 9.98831C11.5721 10.7206 12.025 11.8799 12.025 13.5999C12.025 13.8623 12.2376 14.0749 12.5 14.0749C12.7623 14.0749 12.975 13.8623 12.975 13.5999C12.975 11.72 12.4778 10.2794 11.4959 9.31166C10.7244 8.55135 9.70025 8.12903 8.50625 7.98352C10.0187 7.5474 11.125 6.15289 11.125 4.5C11.125 2.49797 9.50203 0.875 7.5 0.875ZM4.825 4.5C4.825 3.02264 6.02264 1.825 7.5 1.825C8.97736 1.825 10.175 3.02264 10.175 4.5C10.175 5.97736 8.97736 7.175 7.5 7.175C6.02264 7.175 4.825 5.97736 4.825 4.5Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
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
        ))}



        {/* Loading indicator */}
        {isLoading && (
          <div className="flex items-center gap-2">
            <span className="text-xs">🤖</span>
            <div className="px-3 py-2 bg-purple-100 rounded-lg">
              <span className="text-sm text-gray-600">
                AI is typing...
              </span>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
        </div>

        {/* Input Area - Inside white container at bottom */}
        <div className="p-3 border-t border-gray-300 flex gap-2 items-center flex-shrink-0 bg-white w-full">
          <input
            type="text"
            placeholder="Type your message..."
            value={currentMessage}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setCurrentMessage(e.target.value)}
            onKeyPress={handleKeyPress}
            disabled={isLoading}
            className="flex-1 w-full min-w-0 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
          />
          <button 
            onClick={handleSendMessage}
            disabled={!currentMessage.trim() || isLoading}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <svg width="15" height="15" viewBox="0 0 15 15" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M1.20308 1.04312C1.00481 0.954998 0.772341 1.0048 0.627577 1.16641C0.482813 1.32802 0.458794 1.56455 0.568117 1.75196L3.92115 7.50002L0.568117 13.2481C0.458794 13.4355 0.482813 13.672 0.627577 13.8336C0.772341 13.9952 1.00481 14.045 1.20308 13.9569L14.7031 7.95693C14.8836 7.87668 15 7.69762 15 7.50002C15 7.30243 14.8836 7.12337 14.7031 7.04312L1.20308 1.04312ZM4.84553 7.10002L2.21234 2.586L13.2689 7.50002L2.21234 12.414L4.84552 7.90002H9C9.22092 7.90002 9.4 7.72094 9.4 7.50002C9.4 7.27911 9.22092 7.10002 9 7.10002H4.84553Z" fill="currentColor" fillRule="evenodd" clipRule="evenodd"></path>
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};