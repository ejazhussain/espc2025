import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
  TableCellLayout,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { PersonRegular, ChatRegular } from '@fluentui/react-icons';
import { AgentWorkItem, formatWaitTime } from '../../types/workItem';

interface ActiveChatsTableProps {
  items: AgentWorkItem[];
  onSelectChat: (item: AgentWorkItem) => void;
  selectedChatId?: string;
  isDark: boolean;
  loading?: boolean;
}

export const ActiveChatsTable: React.FC<ActiveChatsTableProps> = ({
  items,
  onSelectChat,
  selectedChatId,
  isDark,
  loading = false
}) => {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner label="Loading active chats..." />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className={`flex flex-col items-center justify-center h-64 ${isDark ? 'text-gray-400' : 'text-gray-600'}`}>
        <ChatRegular className="w-16 h-16 mb-4 opacity-50" />
        <p className="text-lg font-medium">No active chats</p>
        <p className="text-sm mt-2">Accept a chat from the Incoming Queue to start</p>
      </div>
    );
  }

  return (
    <div className="overflow-auto">
      <Table
        aria-label="Active chats list"
        className="min-w-full"
      >
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Customer</TableHeaderCell>
            <TableHeaderCell>Agent</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
            <TableHeaderCell>Duration</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const isSelected = item.id === selectedChatId;
            const timeSinceClaimed = item.claimedAt
              ? Math.floor((Date.now() - item.claimedAt.getTime()) / 1000)
              : 0;

            return (
              <TableRow
                key={item.id}
                onClick={() => onSelectChat(item)}
                className={`cursor-pointer hover:bg-opacity-10 ${
                  isSelected
                    ? isDark
                      ? 'bg-purple-900 bg-opacity-20'
                      : 'bg-purple-100 bg-opacity-50'
                    : ''
                }`}
              >
                <TableCell>
                  <TableCellLayout
                    media={
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                        isDark ? 'bg-blue-900' : 'bg-blue-100'
                      }`}>
                        <span className={`text-sm font-medium ${
                          isDark ? 'text-blue-200' : 'text-blue-700'
                        }`}>
                          {item.customerName.split(' ').map(n => n[0]).join('')}
                        </span>
                      </div>
                    }
                  >
                    <div className="font-medium">{item.customerName}</div>
                  </TableCellLayout>
                </TableCell>

                <TableCell>
                  <Badge appearance="outline" color="informative">
                    {item.assignedAgentName || 'Unknown'}
                  </Badge>
                </TableCell>

                <TableCell>
                  {item.status === 'ACTIVE' ? (
                    <Badge appearance="filled" color="success">
                      Active
                    </Badge>
                  ) : item.status === 'CLAIMED' ? (
                    <Badge appearance="filled" color="informative">
                      Claimed
                    </Badge>
                  ) : (
                    <Badge appearance="outline" color="subtle">
                      {item.status}
                    </Badge>
                  )}
                </TableCell>

                <TableCell>
                  <div className="flex items-center gap-1 text-sm">
                    {formatWaitTime(timeSinceClaimed)}
                  </div>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
};
