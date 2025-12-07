// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { makeStyles, tokens } from '@fluentui/react-components';
import { MessageThreadStyles } from '@azure/communication-react';

export const useChatComponentsStyles = makeStyles({
  container: {
    width: '100%',
    height: 'calc(100vh - 60px)',
    display: 'flex',
    flexDirection: 'column'
  },
  messageThreadContainer: {
    flexGrow: 1,
    maxHeight: 'calc(100vh - 60px)',
    overflowY: 'auto'
  },
  sendBoxContainer: {
    padding: '0.75rem 5rem 1.5rem',
    backgroundColor: tokens.colorNeutralBackground3
  },
  richTextSendBox: {
    '& .ms-TextField-fieldGroup': {
      backgroundColor: '#f9fafb !important',
      border: 'none !important',
      borderRadius: '24px !important',
      transition: 'all 0.2s ease !important'
    },
    '& .ms-TextField-field': {
      backgroundColor: 'transparent !important',
      color: '#1f2937 !important'
    },
    '&:focus-within .ms-TextField-fieldGroup': {
      border: '1px solid #d1d5db !important'
    }
  }
});

export const messageThreadStyles = (isDarkMode: boolean): MessageThreadStyles => {
  return {
    chatContainer: {
      backgroundColor: isDarkMode ? '#0f172a' : '#ffffff',
      padding: '1rem 2rem',
      height: '100%',
      overflowY: 'auto',
      display: 'flex',
      flexDirection: 'column'
    },
    myChatMessageContainer: {
      background: isDarkMode ? '#4338ca' : 'rgb(229, 229, 255)',
      borderRadius: '12px',
      margin: '8px 0',
      padding: '12px 16px',
      boxShadow: isDarkMode ? '0 4px 6px -1px rgba(0, 0, 0, 0.3)' : '0 1px 3px rgba(0, 0, 0, 0.1)',
      maxWidth: '50%',
      marginLeft: 'auto',
      marginRight: '0',
      color: isDarkMode ? 'white' : '#374151'
    },
    chatMessageContainer: {
      background: isDarkMode ? '#374151' : '#f8fafc',
      borderRadius: '12px',
      margin: '8px 0',
      padding: '12px 16px',
      boxShadow: isDarkMode ? '0 4px 6px -1px rgba(0, 0, 0, 0.3)' : '0 2px 8px rgba(0, 0, 0, 0.1)',
      maxWidth: '50%',
      marginLeft: '0',
      marginRight: 'auto',
      color: isDarkMode ? '#f3f4f6' : '#1f2937',
      border: isDarkMode ? 'none' : '1px solid #e2e8f0'
    }
  };
};
