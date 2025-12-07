// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Localized strings for the chat application
 * Following Azure best practices for internationalization and accessibility
 */
export const strings = {
  // Chat header strings
  close: 'Close chat',
  endChat: 'End chat',
  online: 'Online',
  offline: 'Offline',
  participants: 'participants',

  // Chat initialization strings
  initializeChatSpinnerLabel: 'Initializing chat...',
  loadingSpinnerLabel: 'Loading...',
  connectingToAgent: 'Connecting to agent...',

  // Chat messages
  conversationResolvedByAgent: 'This conversation has been resolved by the agent.',
  welcomeMessage: 'Welcome to support chat! How can we help you today?',

  // Error messages
  errorMessage: 'An error occurred. Please try again.',
  connectionError: 'Connection error. Please check your internet connection.',
  chatUnavailable: 'Chat is currently unavailable. Please try again later.',

  // Accessibility labels
  chatWindow: 'Chat window',
  messageInput: 'Type your message',
  sendMessage: 'Send message',
  agentAvatar: 'Agent avatar',
  userAvatar: 'User avatar',

  // Status messages
  agentTyping: 'Agent is typing...',
  connecting: 'Connecting...',
  connected: 'Connected',
  disconnected: 'Disconnected',

  // Action buttons
  send: 'Send',
  cancel: 'Cancel',
  retry: 'Retry',
  minimize: 'Minimize',
  maximize: 'Maximize',

  // Configuration screen strings
  chatWithAnExpert: 'Chat with an Expert',
  configurationDisplayNameLabelText: 'Your Name',
  configurationDisplayNamePlaceholder: 'Enter your name',
  configurationQuestionSummaryLabelText: 'What can we help you with?',
  configurationQuestionSummaryPlaceholder: 'Describe your question or issue...',
  requiredTextFiledErrorMessage: 'This field is required',
  startChat: 'Start Chat',
} as const;

/**
 * Chat configuration constants
 */
export const chatConfig = {
  maxMessageLength: 2000,
  typingIndicatorTimeout: 3000,
  connectionTimeout: 30000,
  retryAttempts: 3,
  avatarSize: 32,
} as const;

/**
 * Theme and styling constants
 */
export const themeConstants = {
  borderRadius: '8px',
  shadowLevel1: '0 1px 2px rgba(0, 0, 0, 0.1)',
  shadowLevel2: '0 2px 4px rgba(0, 0, 0, 0.1)',
  transitionDuration: '200ms',
  zIndexHeader: 1000,
  zIndexModal: 2000,
} as const;