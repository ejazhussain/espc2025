// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';
import { ChatTranscript } from '../types/chatTranscript';

export interface SendTranscriptEmailRequest {
  customerEmail: string;
  customerName: string;
  agentName: string;
  threadId: string;
  problemReported: string;
  solutionProvided: string;
  summary: string;
  resolutionDate: string;
}

export interface SendTranscriptEmailResponse {
  success: boolean;
  message: string;
  emailId: string;
}

export const sendTranscriptEmail = async (
  transcript: ChatTranscript,
  customerEmail: string
): Promise<SendTranscriptEmailResponse> => {
  const request: SendTranscriptEmailRequest = {
    customerEmail,
    customerName: transcript.customerName,
    agentName: transcript.agentName,
    threadId: transcript.threadId,
    problemReported: transcript.problemReported,
    solutionProvided: transcript.solutionProvided,
    summary: transcript.summary,
    resolutionDate: transcript.resolutionDate
  };

  try {
    const response = await apiService.post('/email/transcript', request);
    return response.data as SendTranscriptEmailResponse;
  } catch (error: any) {
    // Handle axios error response
    if (error.response?.data) {
      const errorData = error.response.data;
      const errorMessage = errorData?.error || errorData?.message || `HTTP ${error.response.status}: ${error.response.statusText}`;
      throw new Error(errorMessage);
    }
    
    // Handle network or other errors
    throw new Error(error.message || 'Failed to send transcript email');
  }
};