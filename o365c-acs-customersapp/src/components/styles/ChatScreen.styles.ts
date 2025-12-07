// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * ChatScreen styles using Radix UI design system and CSS-in-JS approach
 * Following Azure Communication Services design guidelines
 * Optimized for accessibility, performance, and maintainability
 */

/**
 * Radix UI compatible style definitions
 * Uses CSS custom properties for theming and consistency
 */
export const useChatScreenStyles = () => ({
  chatScreenContainer: {
    display: 'flex',
    flexDirection: 'column' as const,
    height: '100vh',
    maxHeight: '600px',
    width: '100%',
    maxWidth: '400px',
    backgroundColor: 'var(--color-background)',
    borderRadius: 'var(--radius-3)',
    boxShadow: 'var(--shadow-5)',
    overflow: 'hidden',
    border: '1px solid var(--gray-6)',
    fontFamily: 'var(--default-font-family)',
  },

  loadingContainer: {
    display: 'flex',
    flexDirection: 'column' as const,
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    padding: 'var(--space-6)',
    gap: 'var(--space-4)',
    color: 'var(--gray-11)',
  },

  errorContainer: {
    display: 'flex',
    flexDirection: 'column' as const,
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    padding: 'var(--space-6)',
    gap: 'var(--space-4)',
    textAlign: 'center' as const,
    backgroundColor: 'var(--red-1)',
    borderRadius: 'var(--radius-2)',
    margin: 'var(--space-3)',
  },

  errorMessage: {
    fontSize: 'var(--font-size-3)',
    color: 'var(--red-11)',
    fontWeight: '500',
    lineHeight: 'var(--line-height-3)',
    marginBottom: 'var(--space-2)',
  },

  retryButton: {
    marginTop: 'var(--space-4)',
    padding: 'var(--space-2) var(--space-4)',
    backgroundColor: 'var(--accent-9)',
    color: 'var(--accent-9-contrast)',
    border: 'none',
    borderRadius: 'var(--radius-2)',
    fontSize: 'var(--font-size-2)',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s ease',
    
    '&:hover': {
      backgroundColor: 'var(--accent-10)',
    },
    
    '&:active': {
      backgroundColor: 'var(--accent-11)',
    },
    
    '&:focus-visible': {
      outline: '2px solid var(--focus-color)',
      outlineOffset: '2px',
    },
  },

  /**
   * Additional utility styles for enhanced UX
   */
  connectionStatus: {
    position: 'absolute' as const,
    top: 'var(--space-2)',
    right: 'var(--space-2)',
    padding: 'var(--space-1) var(--space-2)',
    fontSize: 'var(--font-size-1)',
    borderRadius: 'var(--radius-1)',
    fontWeight: '500',
  },

  connected: {
    backgroundColor: 'var(--green-3)',
    color: 'var(--green-11)',
  },

  disconnected: {
    backgroundColor: 'var(--orange-3)',
    color: 'var(--orange-11)',
  },

  /**
   * Responsive design for different screen sizes
   */
  '@media (max-width: 480px)': {
    chatScreenContainer: {
      maxHeight: '100vh',
      maxWidth: '100vw',
      borderRadius: '0',
      border: 'none',
    },
  },

  /**
   * High contrast mode support for accessibility
   */
  '@media (prefers-contrast: high)': {
    chatScreenContainer: {
      border: '2px solid var(--gray-12)',
    },
    errorContainer: {
      border: '2px solid var(--red-8)',
    },
  },

  /**
   * Reduced motion support for accessibility
   */
  '@media (prefers-reduced-motion: reduce)': {
    retryButton: {
      transition: 'none',
    },
  },
});