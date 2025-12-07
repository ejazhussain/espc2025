import ApiService, { apiService } from './api';
import { ChatTranscript } from '../types/chatTranscript';

export interface GenerateTranscriptPayload {
	threadId: string;
	customerName?: string;
	agentName?: string;
}

export const generateChatTranscript = async (
	payload: GenerateTranscriptPayload,
	client: ApiService = apiService
): Promise<ChatTranscript> => {
	const response = await client.post('/ai/transcript', payload);
	return response.data as ChatTranscript;
};

export default {
	generateChatTranscript,
};
