// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';

export interface AgentUser {
  teamsUserId: string;
  acsUserId: string;
  displayName: string;
}

const userCache = new Map<string, { user: AgentUser; expiry: number }>();

/**
 * Get the Azure Communication Services user info for a given Teams user.
 */
export const getAgentUser = async (teamsUserId: string): Promise<AgentUser> => {
  try {
    // Check cache first
    const cached = userCache.get(teamsUserId);
    if (cached && Date.now() < cached.expiry) {
      return cached.user;
    }

    const response = await apiService.get(`/agent/getAgentUser?teamsUserId=${teamsUserId}`);
    
    // Cache the user for 30 minutes
    const expiry = Date.now() + (30 * 60 * 1000);
    userCache.set(teamsUserId, { user: response.data, expiry });
    
    return response.data;
  } catch (error) {
    throw new Error('Failed at getting agent ACS user, Error: ' + error);
  }
};

