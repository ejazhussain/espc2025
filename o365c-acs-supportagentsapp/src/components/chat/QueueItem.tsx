// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useState } from 'react';
import { AgentWorkItem } from '../../services/workItemService';
import { Clock, User, AlertTriangle } from 'lucide-react';

interface QueueItemProps {
  workItem: AgentWorkItem;
  onAccept: (threadId: string) => Promise<void>;
  onCancel?: (threadId: string) => Promise<void>;
  isDark: boolean;
}

const formatWaitTime = (seconds: number): string => {
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  if (minutes < 60) {
    return `${minutes}m ${remainingSeconds}s`;
  }
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return `${hours}h ${remainingMinutes}m`;
};

export const QueueItem: React.FC<QueueItemProps> = ({ workItem, onAccept, onCancel, isDark }) => {
  const [accepting, setAccepting] = useState(false);
  const [canceling, setCanceling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const waitTimeSeconds = workItem.waitTimeSeconds || 0;
  const isHighPriority = waitTimeSeconds > 300; // >5 minutes

  const handleAccept = async () => {
    setAccepting(true);
    setError(null);
    try {
      await onAccept(workItem.id);
    } catch (err: any) {
      setError(err.message || 'Failed to accept chat');
    } finally {
      setAccepting(false);
    }
  };

  const handleCancel = async () => {
    if (!onCancel) return;
    
    setCanceling(true);
    setError(null);
    try {
      await onCancel(workItem.id);
    } catch (err: any) {
      setError(err.message || 'Failed to cancel chat request');
    } finally {
      setCanceling(false);
    }
  };

  const bgColor = isDark
    ? isHighPriority
      ? 'bg-red-900/30'
      : 'bg-gray-800'
    : isHighPriority
    ? 'bg-red-50'
    : 'bg-white';

  const borderColor = isHighPriority ? 'border-red-500' : isDark ? 'border-gray-700' : 'border-gray-200';
  const textColor = isDark ? 'text-gray-100' : 'text-gray-900';
  const subtextColor = isDark ? 'text-gray-400' : 'text-gray-600';

  return (
    <div
      className={`${bgColor} ${borderColor} border-2 rounded-lg p-4 mb-3 hover:shadow-lg transition-all ${
        isHighPriority ? 'ring-2 ring-red-300 animate-pulse' : ''
      }`}
    >
      {/* Customer Info */}
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <div className={`rounded-full p-2 ${isDark ? 'bg-gray-700' : 'bg-gray-100'}`}>
            <User size={16} className={subtextColor} />
          </div>
          <span className={`font-semibold ${textColor}`}>{workItem.customerName || 'Unknown Customer'}</span>
        </div>
        {isHighPriority && (
          <span className="text-xs font-bold text-red-600 flex items-center gap-1">
            <AlertTriangle size={14} />
            HIGH PRIORITY
          </span>
        )}
      </div>

      {/* Wait Time */}
      <div className={`text-sm ${subtextColor} mb-3 flex items-center gap-1`}>
        <Clock size={14} />
        Waiting: {formatWaitTime(waitTimeSeconds)}
      </div>

      {/* Priority Warning */}
      {isHighPriority && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-300 dark:border-red-700 rounded p-2 mb-3 text-sm text-red-800 dark:text-red-200">
          This customer has been waiting too long. Please accept immediately!
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-300 dark:border-red-700 rounded p-2 mb-3 text-sm text-red-800 dark:text-red-200">
          {error}
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex gap-2">
        {/* Accept Button */}
        <button
          onClick={handleAccept}
          disabled={accepting || canceling}
          className={`flex-1 py-2 px-4 rounded-lg font-semibold transition-colors ${
            isHighPriority
              ? 'bg-red-600 hover:bg-red-700 text-white'
              : isDark
              ? 'bg-blue-600 hover:bg-blue-700 text-white'
              : 'bg-blue-600 hover:bg-blue-700 text-white'
          } disabled:opacity-50 disabled:cursor-not-allowed`}
        >
          {accepting ? 'Accepting...' : isHighPriority ? 'Accept Urgent' : 'Accept Chat'}
        </button>

        {/* Cancel Button */}
        {onCancel && (
          <button
            onClick={handleCancel}
            disabled={accepting || canceling}
            className={`py-2 px-4 rounded-lg font-semibold transition-colors ${
              isDark
                ? 'bg-gray-700 hover:bg-gray-600 text-gray-200'
                : 'bg-gray-200 hover:bg-gray-300 text-gray-700'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {canceling ? 'Canceling...' : 'Cancel'}
          </button>
        )}
      </div>
    </div>
  );
};
