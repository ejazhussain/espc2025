// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * ChatComponents styles using Radix UI design system
 * Following Azure Communication Services design guidelines
 * Optimized for accessibility, performance, and maintainability
 */

/**
 * Radix UI compatible style definitions
 * Uses CSS custom properties for theming and consistency
 */
export const useChatComponentsStyles = () => ({
  container: {
    width: '100%',
    height: '100%', // Use full available height
    display: 'flex',
    flexDirection: 'column' as const,
    backgroundColor: 'var(--color-background)',
    borderRadius: 'var(--radius-2)',
    overflow: 'hidden'
  },
  messageThreadContainer: {
    flex: 1, // Take all available space
    overflowY: 'auto' as const,
    overflowX: 'hidden' as const,
    padding: '8px 8px 8px 8px', // Add consistent padding around the chat
    minHeight: 0 // Important for flex child with overflow
  },

  sendBoxContainer: {
    backgroundColor: 'transparent',
    minHeight: '64px',
    maxHeight: '64px', // Fixed height for input area
    display: 'flex',
    alignItems: 'center',
    flexShrink: 0 // Don't shrink the input area
  },

  resolveSystemMessageContainer: {
    display: 'flex',
    justifyContent: 'center',
    padding: 'var(--space-3) var(--space-2)',
    margin: 'var(--space-2) 0'
  },

  resolveSystemMessage: {
    fontWeight: '400',
    fontSize: 'var(--font-size-1)',
    letterSpacing: '0',
    color: 'var(--gray-11)',
    backgroundColor: 'var(--amber-2)',
    padding: 'var(--space-2) var(--space-3)',
    borderRadius: 'var(--radius-2)',
    border: '1px solid var(--amber-6)',
    textAlign: 'center' as const,
    maxWidth: '280px',
    lineHeight: 'var(--line-height-2)',
    boxShadow: 'var(--shadow-1)'
  },

  /**
   * Avatar styling for message display
   */
  avatar: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    verticalAlign: 'middle',
    overflow: 'hidden',
    userSelect: 'none' as const,
    borderRadius: '50%',
    backgroundColor: 'var(--accent-9)',
    color: 'var(--accent-9-contrast)',
    fontWeight: '500',
    border: '2px solid var(--color-background)',
    boxShadow: 'var(--shadow-2)'
  },

  /**
   * Additional utility styles for enhanced UX
   */
  messageContainer: {
    marginBottom: 'var(--space-2)',
    padding: 'var(--space-1)',
    borderRadius: 'var(--radius-1)',
    transition: 'background-color 0.2s ease'
  },

  messageHover: {
    backgroundColor: 'var(--gray-2)'
  },

  /**
   * Responsive design for different screen sizes
   */
  '@media (max-width: 480px)': {
    container: {
      height: 'calc(100vh - 60px)' // Adjust for mobile header
    },
    sendBoxContainer: {
      padding: 'var(--space-2)'
    },
    messageThreadContainer: {
      padding: 'var(--space-1)'
    }
  },

  /**
   * High contrast mode support for accessibility
   */
  '@media (prefers-contrast: high)': {
    resolveSystemMessage: {
      border: '2px solid var(--amber-8)',
      backgroundColor: 'var(--amber-3)'
    },
    sendBoxContainer: {
      borderTop: '2px solid var(--gray-8)'
    }
  },

  /**
   * Reduced motion support for accessibility
   */
  '@media (prefers-reduced-motion: reduce)': {
    messageContainer: {
      transition: 'none'
    }
  }
});

/**
 * Message thread styles for Azure Communication Services
 * Compatible with ACS React SDK styling requirements
 * Enhanced with modern bubble-style messages
 * Using CSS-in-JS with higher specificity to override default ACS styles
 */
export const messageThreadStyles = {
  chatContainer: {
    padding: '0 !important',
    width: '100% !important',
    fontFamily: 'var(--default-font-family)',
    fontSize: 'var(--font-size-2)',
    lineHeight: 'var(--line-height-2)',
    backgroundColor: 'transparent !important',
  },

  messageContainer: {
    marginBottom: '4px !important',
    padding: '0 !important',
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },

  // Modern bubble-style messages with !important to override ACS defaults
  message: {
    maxWidth: '80% !important',
    padding: '6px 12px !important',
    borderRadius: '16px !important',
    fontSize: 'var(--font-size-2) !important',
    lineHeight: 'var(--line-height-3) !important',
    wordWrap: 'break-word !important',
    position: 'relative',
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.1) !important',
    transition: 'all 0.2s ease',
    margin: '2px 0 !important',
  },

  // User messages (right-aligned, blue) - force override ACS styles
  messageFromSelf: {
    backgroundColor: 'var(--blue-9) !important',
    color: 'white !important',
    alignSelf: 'flex-end !important',
    borderBottomRightRadius: '4px !important',
    marginLeft: 'auto !important',
    marginRight: '0 !important',
    boxShadow: '0 4px 12px rgba(59, 130, 246, 0.3) !important',
  },

  // Agent messages (left-aligned, white) - force override ACS styles
  messageFromOthers: {
    backgroundColor: 'white !important',
    color: 'var(--gray-12) !important',
    alignSelf: 'flex-start !important',
    borderBottomLeftRadius: '4px !important',
    border: '1px solid var(--gray-4) !important',
    marginRight: 'auto !important',
    marginLeft: '0 !important',
  },

  messageContent: {
    margin: '0 !important',
    padding: '0 !important',
    wordWrap: 'break-word !important',
    overflowWrap: 'break-word !important',
  },

  messageTime: {
    fontSize: 'var(--font-size-1) !important',
    opacity: '0.7 !important',
    marginTop: '2px !important',
    textAlign: 'right !important',
    color: 'inherit !important',
  },

  messageAuthor: {
    fontSize: 'var(--font-size-1) !important',
    fontWeight: '500 !important',
    marginBottom: '2px !important',
    opacity: '0.8 !important',
  },

  // System messages
  systemMessage: {
    alignSelf: 'center !important',
    backgroundColor: 'var(--amber-2) !important',
    color: 'var(--amber-11) !important',
    border: '1px solid var(--amber-6) !important',
    fontSize: 'var(--font-size-1) !important',
    maxWidth: '280px !important',
    textAlign: 'center !important',
    fontStyle: 'italic !important',
    margin: '4px auto !important',
  },

  // Avatar styling with forced overrides
  avatar: {
    width: '28px !important',
    height: '28px !important',
    borderRadius: '50% !important',
    border: '2px solid white !important',
    boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1) !important',
    marginBottom: '2px !important',
    marginLeft: '8px !important', // Add left margin for proper alignment
  },

  // Modern send box styling
  sendBox: {
    padding: 'var(--space-3) !important',
    backgroundColor: 'white !important',
    borderRadius: 'var(--radius-4) !important',
    border: '2px solid var(--gray-4) !important',
    margin: 'var(--space-2) !important',
    boxShadow: '0 4px 16px rgba(0, 0, 0, 0.08) !important',
    transition: 'all 0.2s ease !important',
  },

  sendBoxFocused: {
    borderColor: 'var(--blue-8) !important',
    boxShadow: '0 6px 24px rgba(59, 130, 246, 0.15) !important',
    transform: 'translateY(-2px) !important',
  },

  sendBoxInput: {
    border: 'none !important',
    outline: 'none !important',
    fontSize: 'var(--font-size-2) !important',
    padding: 'var(--space-2) !important',
    width: '100% !important',
    backgroundColor: 'transparent !important',
    color: 'var(--gray-12) !important',
    fontFamily: 'inherit !important',
  },

  sendButton: {
    backgroundColor: 'var(--blue-9) !important',
    color: 'white !important',
    border: 'none !important',
    borderRadius: 'var(--radius-3) !important',
    padding: 'var(--space-2) var(--space-3) !important',
    fontSize: 'var(--font-size-2) !important',
    fontWeight: '500 !important',
    cursor: 'pointer !important',
    transition: 'all 0.2s ease !important',
    marginLeft: 'var(--space-2) !important',
  },

  sendButtonHover: {
    backgroundColor: 'var(--blue-10) !important',
    transform: 'scale(1.05) !important',
  },

  sendButtonActive: {
    transform: 'scale(0.95) !important',
  },

  sendButtonDisabled: {
    backgroundColor: 'var(--gray-6) !important',
    cursor: 'not-allowed !important',
    transform: 'none !important',
  },
};