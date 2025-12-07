// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';

export type UserToken = {
  expiresOn: number;
  identity: string;
  token: string;
};

const tokenCache = new Map<string, { token: UserToken; expiry: number }>();

/**
 * This gets the token for a given user
 */
export const getToken: (userId: string) => Promise<UserToken> = async (userId?: string) => {
  try {
    // Check cache first
    const cached = tokenCache.get(userId || '');
    if (cached && Date.now() < cached.expiry) {
      return cached.token;
    }

    const response = await apiService.get(`/token/info?userId=${userId}`);
    
    const userToken: UserToken = {
      expiresOn: response.data.expiresOn,
      identity: response.data.user.communicationUserId,
      token: response.data.token
    };

    // Cache the token for 50 minutes (assuming 1 hour expiry with 10 min buffer)
    const expiry = Date.now() + (50 * 60 * 1000);
    tokenCache.set(userId || '', { token: userToken, expiry });
    
    return userToken;
  } catch (error) {
    throw new Error('could not get token for user: ' + userId + ' due to error: ' + error);
  }
};
