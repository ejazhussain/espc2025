// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { createContext, useContext, ReactNode } from 'react';
import { 
  useId, 
  useToastController, 
  Toast, 
  ToastTitle, 
  ToastBody, 
  Toaster
} from '@fluentui/react-components';

export interface NotificationOptions {
  type?: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
  position?: 'top' | 'bottom' | 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
}

export interface NotificationContextType {
  showNotification: (title: string, message?: string, options?: NotificationOptions) => void;
  showSuccess: (title: string, message?: string) => void;
  showError: (title: string, message?: string) => void;
  showWarning: (title: string, message?: string) => void;
  showInfo: (title: string, message?: string) => void;
}

const NotificationContext = createContext<NotificationContextType | null>(null);

interface NotificationProviderProps {
  children: ReactNode;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const toasterId = useId('global-notification-toaster');
  const { dispatchToast } = useToastController(toasterId);

  const showNotification = (
    title: string, 
    message?: string, 
    options: NotificationOptions = {}
  ) => {
    const {
      type = 'info',
      duration = 5000,
      position = 'top'
    } = options;

    // Map types to FluentUI toast intents
    const intentMap = {
      success: 'success' as const,
      error: 'error' as const,
      warning: 'warning' as const,
      info: 'info' as const
    };

    dispatchToast(
      <Toast>
        <ToastTitle>{title}</ToastTitle>
        {message && <ToastBody>{message}</ToastBody>}
      </Toast>,
      {
        intent: intentMap[type],
        position,
        timeout: duration
      }
    );
  };

  const showSuccess = (title: string, message?: string) => {
    showNotification(title, message, { type: 'success' });
  };

  const showError = (title: string, message?: string) => {
    showNotification(title, message, { type: 'error', duration: 7000 });
  };

  const showWarning = (title: string, message?: string) => {
    showNotification(title, message, { type: 'warning' });
  };

  const showInfo = (title: string, message?: string) => {
    showNotification(title, message, { type: 'info' });
  };

  const contextValue: NotificationContextType = {
    showNotification,
    showSuccess,
    showError,
    showWarning,
    showInfo
  };

  return (
    <NotificationContext.Provider value={contextValue}>
      {children}
      {/* Global Toaster component */}
      <Toaster toasterId={toasterId} />
    </NotificationContext.Provider>
  );
};

// Custom hook to use notifications
export const useNotifications = (): NotificationContextType => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

export default NotificationProvider;
