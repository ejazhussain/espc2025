// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// API Service
export { apiService, default as ApiService } from './api';

// Endpoint Service
export { getEndpointUrl } from './endpointService';

// Auth Service (renamed from tokenService)
export { 
  getToken, 
  type UserToken 
} from './authService';

// User Service (renamed from agentUserService)
export { 
  getAgentUser as getAgentACSUser, 
  type AgentUser
} from './userService';

// Work Item Service
export { 
  getAgentWorkItems, 
  createAgentWorkItem,
  updateAgentWorkItem,
  getCancelledWorkItems,
  type AgentWorkItem
} from './workItemService';

// Thread Service
export { 
  ThreadService, 
  default as ThreadServiceDefault 
} from './threadService';

// Summary / Transcript Service
export {
  generateChatTranscript,
  type GenerateTranscriptPayload
} from './transcriptService';
