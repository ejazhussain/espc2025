import React, { useState, useEffect, useRef } from 'react';
import { InputSection } from './InputSection';
import { LoadingSpinner } from './LoadingSpinner';
import { strings } from './utils/constants';
import { createAndJoinChatThreadWithNewUser } from './createAndJoinChatThreadWithNewUser';

interface EscalationData {
  conversationId: string;
  customerName: string;
  customerEmail: string;
  messages: any[];
  problemDescription: string;
}

export interface ConfigurationScreenProps {
  onJoinChat(): void;
  setToken(token: string): void;
  setUserId(userId: string): void;
  setDisplayName(displayName: string): void;
  setThreadId(threadId: string): void;
  setEndpointUrl(endpointUrl: string): void;
  setAgentName(agentName: string): void;
  onCloseButtonClicked(): void;
  onError(error: string, questionSummary: string): void;
  userDisplayName?: string; // Add user display name prop
  userEmail?: string; // Add user email prop
  escalationData?: EscalationData; // Add escalation data for auto-submission
}

export const  ConfigurationScreen: React.FC<ConfigurationScreenProps> = (props) => {
  const {
    onJoinChat,
    setToken,
    setUserId,
    setDisplayName,
    setThreadId,
    setEndpointUrl,
    setAgentName,
    onCloseButtonClicked,
    onError,
    userDisplayName,
    userEmail,
    escalationData
  } = props;

  const [name, setName] = useState(escalationData?.customerName || userDisplayName || ''); // Pre-populate with escalation data first
  const [emptyNameWarning, setEmptyNameWarning] = useState(false);
  const [email, setEmail] = useState(escalationData?.customerEmail || userEmail || ''); // Pre-populate with escalation data first
  const [emptyEmailWarning, setEmptyEmailWarning] = useState(false);
  const [invalidEmailWarning, setInvalidEmailWarning] = useState(false);
  const [questionSummary, setQuestionSummary] = useState(escalationData?.problemDescription || ''); // Pre-populate with problem description
  const [emptyQuestionSummaryWarning, setEmptyQuestionSummaryWarning] = useState(false);
  const [disableJoinChatButton, setDisableJoinChatButton] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  
  // Ref to track if escalation has already been processed to prevent duplicate calls
  const escalationProcessedRef = useRef(false);

  const isValidEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  const validateRequiredFields = (): boolean => {
    if (!name) {
      setEmptyNameWarning(true);
    }
    if (!email) {
      setEmptyEmailWarning(true);
    } else if (!isValidEmail(email)) {
      setInvalidEmailWarning(true);
    }
    if (!questionSummary) {
      setEmptyQuestionSummaryWarning(true);
    }
    return !!name && !!email && isValidEmail(email) && !!questionSummary;
  };

  const startChat = (): void => {
    const validRequiredFields = validateRequiredFields();
    if (!validRequiredFields) {
      return;
    }
    setEmptyNameWarning(false);
    setEmptyEmailWarning(false);
    setInvalidEmailWarning(false);
    setEmptyQuestionSummaryWarning(false);
    setDisableJoinChatButton(true);
    setIsLoading(true);
    setDisplayName(name);

    createAndJoinChatThreadWithNewUser({
      displayName: name,
      customerEmail: email,
      questionSummary,
      conversationHistory: undefined, // No conversation history for regular chat
      onJoinChat,
      setToken,
      setUserId,
      setThreadId,
      setEndpointUrl,
      setAgentName,
      onError
    });
  };

  // Auto-submit when escalation data is provided
  useEffect(() => {
    if (escalationData && 
        escalationData.customerName && 
        escalationData.customerEmail && 
        escalationData.problemDescription && 
        !isLoading && 
        !escalationProcessedRef.current) {
      
      // Mark escalation as processed to prevent duplicate calls
      escalationProcessedRef.current = true;
      
      console.log('[Configuration] Auto-submitting escalation data:', {
        customerName: escalationData.customerName,
        customerEmail: escalationData.customerEmail,
        conversationId: escalationData.conversationId
      });
      
      // Auto-submit directly without showing the form
      setEmptyNameWarning(false);
      setEmptyEmailWarning(false);
      setInvalidEmailWarning(false);
      setEmptyQuestionSummaryWarning(false);
      setDisableJoinChatButton(true);
      setIsLoading(true);
      setDisplayName(escalationData.customerName);

      createAndJoinChatThreadWithNewUser({
        displayName: escalationData.customerName,
        customerEmail: escalationData.customerEmail,
        questionSummary: escalationData.problemDescription,
        conversationHistory: escalationData.messages, // Pass the full conversation history
        onJoinChat,
        setToken,
        setUserId,
        setThreadId,
        setEndpointUrl,
        setAgentName,
        onError
      });
    }
  }, [escalationData, isLoading, onJoinChat, setToken, setUserId, setThreadId, setEndpointUrl, setAgentName, onError, setDisplayName]);

  const renderConfigurationView = (): JSX.Element => {
    return (
      <div className="p-5 h-full flex flex-col">
        {/* Header with close button */}
        <div className="flex justify-between items-center mb-5">
          <h2 className="text-xl font-bold">💬 {strings.chatWithAnExpert}</h2>
          <button
            onClick={onCloseButtonClicked}
            aria-label={strings.close}
            className="text-gray-500 hover:text-gray-700 text-xl px-2"
          >
            ✕
          </button>
        </div>

        {/* Input fields */}
        <div className="flex-1 flex flex-col gap-4 mx-2">
          <InputSection
            labelText={strings.configurationDisplayNameLabelText}
            placeholder={strings.configurationDisplayNamePlaceholder}
            isEmpty={emptyNameWarning}
            emptyErrorMessage={strings.requiredTextFiledErrorMessage}
            onTextChangedHandler={(newValue) => {
              setName(newValue);
              setEmptyNameWarning(!newValue);
            }}
            onKeyDownHandler={startChat}
            value={name} // Pass the name value to display pre-populated user name
          />

          <InputSection
            labelText="Email Address"
            placeholder="your.email@company.com"
            isEmpty={emptyEmailWarning || invalidEmailWarning}
            emptyErrorMessage={emptyEmailWarning ? strings.requiredTextFiledErrorMessage : "Please enter a valid email address"}
            onTextChangedHandler={(newValue) => {
              setEmail(newValue);
              setEmptyEmailWarning(!newValue);
              setInvalidEmailWarning(!!newValue && !isValidEmail(newValue));
            }}
            onKeyDownHandler={startChat}
            value={email}
            readonly={!!userEmail} // Make readonly if user email is available
          />

          <InputSection
            labelText={strings.configurationQuestionSummaryLabelText}
            placeholder={strings.configurationQuestionSummaryPlaceholder}
            isEmpty={emptyQuestionSummaryWarning}
            emptyErrorMessage={strings.requiredTextFiledErrorMessage}
            onTextChangedHandler={(newValue) => {
              setQuestionSummary(newValue);
              setEmptyQuestionSummaryWarning(!newValue);
            }}
            onKeyDownHandler={startChat}
            isMultiline={true}
            value={questionSummary} // Pass the question summary value for consistency
          />

          <button
            disabled={disableJoinChatButton}
            onClick={startChat}
            className="px-4 py-3 bg-gradient-to-br from-primary-600 to-secondary-600 text-white rounded-lg hover:opacity-90 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed mt-auto"
          >
            {strings.startChat}
          </button>
        </div>
      </div>
    );
  };

  return isLoading ? <LoadingSpinner label={strings.initializeChatSpinnerLabel} /> : renderConfigurationView();
};