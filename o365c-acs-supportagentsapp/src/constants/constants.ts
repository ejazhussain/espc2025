// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export enum StatusCode {
  OK = 200,
  CREATED = 201,
  NOTFOUND = 404
}

export const ENTER_KEY = 13;

export interface ThreadStrings {
  /** String for Sunday */
  sunday: string;
  /** String for Monday */
  monday: string;
  /** String for Tuesday */
  tuesday: string;
  /** String for Wednesday */
  wednesday: string;
  /** String for Thursday */
  thursday: string;
  /** String for Friday */
  friday: string;
  /** String for Saturday */
  saturday: string;
  /** String for Yesterday */
  yesterday: string;
  /** String for Close button text */
  close: string;
  /** String for Resolve button text */
  resolve: string;
  /** String for the error screen title */
  errorScreenTitle: string;
  /** String for the error message of ACS user not linked */
  failToLinkToACSUser: string;
  /** String for the error message of failed to get the Teams user information */
  failToGetTeamsUserInfo: string;
  /** String for no threads */
  noThreads: string;
  /** String for assigned to me label on the thread list */
  assignedToMe: string;
  /** String for the title of the toast notification when a thread is resolved */
  resolvedToasterTitle: string;
  /** String for the view button text in the resolved thread toaster */
  resolvedToasterViewButton: string;
  /** String for successful thread deletion notification */
  threadDeletedSuccessTitle: string;
  /** String for failed thread deletion notification */
  threadDeleteFailedTitle: string;
}

export const threadStrings: ThreadStrings = {
  sunday: 'Sunday',
  monday: 'Monday',
  tuesday: 'Tuesday',
  wednesday: 'Wednesday',
  thursday: 'Thursday',
  friday: 'Friday',
  saturday: 'Saturday',
  yesterday: 'Yesterday',
  close: 'Close',
  resolve: 'Resolve',
  errorScreenTitle: 'Configuration error',
  failToLinkToACSUser: 'ACS user not linked.',
  failToGetTeamsUserInfo: 'Failed to get Teams user information',
  noThreads: 'No chats',
  assignedToMe: 'Assigned to me',
  resolvedToasterTitle: 'Thread resolved',
  resolvedToasterViewButton: 'View',
  threadDeletedSuccessTitle: 'Thread Deleted',
  threadDeleteFailedTitle: 'Delete Failed'
};

export const apiEndpoints = {
  getEndpointUrl: '/getEndpointUrl',
  getToken: '/getToken',
  getAgentACSUser: '/getAgentACSUser',
  createAgentACSUser: '/createAgentACSUser',
  updateAgentACSUser: '/updateAgentACSUser',
  getAgentWorkItems: '/getAgentWorkItems',
  getWorkItem: '/getWorkItem',
  createWorkItem: '/createWorkItem',
  updateWorkItem: '/updateWorkItem',
  deleteWorkItem: '/deleteWorkItem',
  assignWorkItem: '/assignWorkItem',
  resolveWorkItem: '/resolveWorkItem',
};

export const cacheExpiryTimes = {
  token: 50 * 60 * 1000, // 50 minutes
  user: 30 * 60 * 1000, // 30 minutes
  workItems: 5 * 60 * 1000, // 5 minutes
};

export const retryConfig = {
  maxRetries: 3,
  baseDelay: 1000,
};

export const httpStatusCodes = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  INTERNAL_SERVER_ERROR: 500,
} as const;

