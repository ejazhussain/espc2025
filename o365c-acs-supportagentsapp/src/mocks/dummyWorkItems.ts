import { AgentWorkItem } from '../services/workItemService';
import { ThreadItemStatus } from '../components/chat/useThreads';

// Simulate incoming chat requests (UNASSIGNED - waiting for agent to accept)
export const DUMMY_INCOMING_QUEUE: AgentWorkItem[] = [
  {
    id: 'thread-incoming-1',
    customerName: 'Alan Partridge',
    createdAt: new Date(Date.now() - 42000).toISOString(), // 42s ago
    updatedAt: new Date(Date.now() - 42000).toISOString(),
    waitTimeSeconds: 42,
    status: ThreadItemStatus.UNASSIGNED,
    priority: 'NORMAL'
  },
  {
    id: 'thread-incoming-2',
    customerName: 'Sarah Johnson',
    createdAt: new Date(Date.now() - 185000).toISOString(), // 3m 5s ago
    updatedAt: new Date(Date.now() - 185000).toISOString(),
    waitTimeSeconds: 185,
    status: ThreadItemStatus.UNASSIGNED,
    priority: 'NORMAL'
  },
  {
    id: 'thread-incoming-3',
    customerName: 'Mike Chen',
    createdAt: new Date(Date.now() - 420000).toISOString(), // 7m ago
    updatedAt: new Date(Date.now() - 420000).toISOString(),
    waitTimeSeconds: 420,
    status: ThreadItemStatus.UNASSIGNED,
    priority: 'HIGH' // High priority due to long wait (>5min)
  }
];

// Simulate active chats claimed by current agent
export const DUMMY_MY_ACTIVE: AgentWorkItem[] = [
  {
    id: 'thread-active-1',
    customerName: 'David Lee',
    createdAt: new Date(Date.now() - 900000).toISOString(), // 15m ago
    updatedAt: new Date(Date.now() - 900000).toISOString(),
    waitTimeSeconds: 900,
    status: ThreadItemStatus.ACTIVE,
    priority: 'NORMAL',
    assignedAgentId: 'current-agent',
    assignedAgentName: 'You',
    claimedAt: new Date(Date.now() - 870000).toISOString() // Claimed 14.5m ago
  },
  {
    id: 'thread-active-2',
    customerName: 'Emily Brown',
    createdAt: new Date(Date.now() - 120000).toISOString(), // 2m ago
    updatedAt: new Date(Date.now() - 120000).toISOString(),
    waitTimeSeconds: 120,
    status: ThreadItemStatus.CLAIMED,
    priority: 'NORMAL',
    assignedAgentId: 'current-agent',
    assignedAgentName: 'You',
    claimedAt: new Date(Date.now() - 30000).toISOString() // Claimed 30s ago
  }
];

// Simulate resolved chats
export const DUMMY_RESOLVED: AgentWorkItem[] = [
  {
    id: 'thread-resolved-1',
    customerName: 'Robert Smith',
    createdAt: new Date(Date.now() - 3600000).toISOString(), // 1h ago
    updatedAt: new Date(Date.now() - 3600000).toISOString(),
    waitTimeSeconds: 1200,
    status: ThreadItemStatus.RESOLVED,
    priority: 'NORMAL',
    assignedAgentId: 'current-agent',
    assignedAgentName: 'You',
    claimedAt: new Date(Date.now() - 3570000).toISOString()
  },
  {
    id: 'thread-resolved-2',
    customerName: 'Jennifer Wilson',
    createdAt: new Date(Date.now() - 7200000).toISOString(), // 2h ago
    updatedAt: new Date(Date.now() - 7200000).toISOString(),
    waitTimeSeconds: 600,
    status: ThreadItemStatus.RESOLVED,
    priority: 'NORMAL',
    assignedAgentId: 'current-agent',
    assignedAgentName: 'You',
    claimedAt: new Date(Date.now() - 7100000).toISOString()
  }
];
