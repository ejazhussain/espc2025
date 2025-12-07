// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ThreadListHeader } from './ThreadListHeader';
import { AgentWorkItem } from '../../services/workItemService';
import { User, Clock, XCircle } from 'lucide-react';

interface CancelledItemsPanelProps {
  cancelledItems: AgentWorkItem[];
  tabs: string[];
  selectedTab: string;
  onStatusTabSelected: (tabValue: string) => void;
  isDark?: boolean;
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

const formatDate = (date: Date | string): string => {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
};

export const CancelledItemsPanel = ({
  cancelledItems,
  tabs,
  selectedTab,
  onStatusTabSelected,
  isDark = true
}: CancelledItemsPanelProps): JSX.Element => {
  
  const bgColor = isDark ? 'bg-gray-900' : 'bg-gray-50';
  const textColor = isDark ? 'text-gray-100' : 'text-gray-900';
  const subtextColor = isDark ? 'text-gray-400' : 'text-gray-600';

  return (
    <div className={`h-full flex flex-col ${bgColor}`}>
      {/* Header with tabs */}
      <div className={`${isDark ? 'border-b border-gray-700' : 'border-b border-gray-100'} p-6`}>
        <ThreadListHeader
          tabs={tabs}
          selectedTab={selectedTab}
          onStatusTabSelected={onStatusTabSelected}
          isDark={isDark}
        />
      </div>

      {/* Cancelled Items List */}
      <div className="flex-1 overflow-y-auto p-4">
        {cancelledItems.length === 0 ? (
          <div className="text-center py-8">
            <XCircle size={48} className={`mx-auto mb-3 ${subtextColor}`} />
            <p className={`text-lg font-medium ${textColor}`}>No cancelled chats</p>
            <p className={`text-sm ${subtextColor} mt-2`}>Cancelled conversations will appear here</p>
          </div>
        ) : (
          cancelledItems.map((item) => {
            const cardBgColor = isDark ? 'bg-gray-800' : 'bg-white';
            const borderColor = isDark ? 'border-gray-700' : 'border-gray-200';

            return (
              <div
                key={item.id}
                className={`${cardBgColor} ${borderColor} border-2 rounded-lg p-4 mb-3 hover:shadow-lg transition-all`}
              >
                {/* Customer Info */}
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <div className={`rounded-full p-2 ${isDark ? 'bg-gray-700' : 'bg-gray-100'}`}>
                      <User size={16} className={subtextColor} />
                    </div>
                    <span className={`font-semibold ${textColor}`}>
                      {item.customerName || 'Unknown Customer'}
                    </span>
                  </div>
                  <span className="px-2 py-1 text-xs font-medium rounded-full bg-red-500/20 text-red-400 border border-red-500/30">
                    Cancelled
                  </span>
                </div>

                {/* Details */}
                <div className={`text-sm ${subtextColor} space-y-1`}>
                  <div className="flex items-center gap-1">
                    <Clock size={14} />
                    <span>Wait time: {formatWaitTime(item.waitTimeSeconds || 0)}</span>
                  </div>
                  <div>Created: {formatDate(item.createdAt)}</div>
                  {item.updatedAt && <div>Updated: {formatDate(item.updatedAt)}</div>}
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
};
