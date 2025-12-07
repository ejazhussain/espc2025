export interface AgentWorkItem {
  id: string;
  customerName: string;
  createdAt: Date;
  waitTimeSeconds: number;
  status: WorkItemStatus;
  priority: WorkItemPriority;
  assignedAgentId?: string;
  assignedAgentName?: string;
  claimedAt?: Date;
}

export type WorkItemStatus = 'UNASSIGNED' | 'CLAIMED' | 'ACTIVE' | 'RESOLVED';
export type WorkItemPriority = 'normal' | 'high';

// Helper function to format wait time
export const formatWaitTime = (seconds: number): string => {
  if (seconds < 60) {
    return `${seconds}s`;
  } else if (seconds < 3600) {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return secs > 0 ? `${minutes}m ${secs}s` : `${minutes}m`;
  } else {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
  }
};

// Helper function to determine if item is high priority (>5min wait)
export const isHighPriority = (waitTimeSeconds: number): boolean => {
  return waitTimeSeconds > 300; // 5 minutes
};
