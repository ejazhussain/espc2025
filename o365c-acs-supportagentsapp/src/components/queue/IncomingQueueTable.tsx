import React from 'react';
import {
  Button,
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
import { PersonRegular, ClockRegular, WarningRegular } from '@fluentui/react-icons';
import { AgentWorkItem, formatWaitTime } from '../../types/workItem';

interface IncomingQueueTableProps {
  items: AgentWorkItem[];
  onAccept: (item: AgentWorkItem) => void;
  isDark: boolean;
  loading?: boolean;
}

export const IncomingQueueTable: React.FC<IncomingQueueTableProps> = ({
  items,
  onAccept,
  isDark,
  loading = false
}) => {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner label="Loading incoming requests..." />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className={`flex flex-col items-center justify-center h-64 ${isDark ? 'text-gray-400' : 'text-gray-600'}`}>
        <PersonRegular className="w-16 h-16 mb-4 opacity-50" />
        <p className="text-lg font-medium">No incoming requests</p>
        <p className="text-sm mt-2">New customer requests will appear here</p>
      </div>
    );
  }

  return (
    <div className="overflow-auto">
      <Table
        aria-label="Incoming chat requests queue"
        className="min-w-full"
      >
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Customer</TableHeaderCell>
            <TableHeaderCell>Wait Time</TableHeaderCell>
            <TableHeaderCell>Priority</TableHeaderCell>
            <TableHeaderCell>Action</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const isHighPriority = item.priority === 'high';

            return (
              <TableRow key={item.id}>
                <TableCell>
                  <TableCellLayout
                    media={
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                        isDark ? 'bg-purple-900' : 'bg-purple-100'
                      }`}>
                        <span className={`text-sm font-medium ${
                          isDark ? 'text-purple-200' : 'text-purple-700'
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
                  <div className={`flex items-center gap-1 ${
                    isHighPriority ? 'text-red-600 font-semibold' : ''
                  }`}>
                    <ClockRegular className="w-4 h-4" />
                    <span>{formatWaitTime(item.waitTimeSeconds)}</span>
                  </div>
                </TableCell>

                <TableCell>
                  {isHighPriority ? (
                    <Badge
                      appearance="filled"
                      color="danger"
                      icon={<WarningRegular />}
                    >
                      HIGH
                    </Badge>
                  ) : (
                    <Badge appearance="outline" color="subtle">
                      Normal
                    </Badge>
                  )}
                </TableCell>

                <TableCell>
                  <Button
                    appearance="primary"
                    onClick={() => onAccept(item)}
                  >
                    Accept Chat
                  </Button>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
};
