/**
 * Teams Meeting Integration - Support Agent App Implementation
 * 
 * This file contains the complete implementation for agent-side
 * Teams meeting creation and management functionality
 */

import { useState } from 'react';
import axios from 'axios';
import { useNotifications } from '../contexts/NotificationContext';

// ============================================================================
// Types & Interfaces
// ============================================================================

interface AgentChatSession {
  threadId: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  agentId: string;
  agentName: string;
  agentEmail: string;
}

interface CreateMeetingRequest {
  threadId: string;
  customerName: string;
  customerEmail: string;
  agentEmail: string;
  startDateTime: string;
  endDateTime: string;
  subject: string;
  description: string;
  timeZone?: string;
}

interface CreateMeetingResponse {
  success: boolean;
  eventId: string;
  joinUrl: string;
  conferenceId: string | null;
  startDateTime: string;
  endDateTime: string;
  threadId: string;
  errorMessage: string | null;
}

interface MeetingSchedulerProps {
  chatSession: AgentChatSession;
  onMeetingCreated: (meeting: CreateMeetingResponse) => void;
}

interface MeetingControlsProps {
  meeting: CreateMeetingResponse;
  onMeetingCancelled: () => void;
}

// ============================================================================
// API Service
// ============================================================================

class MeetingService {
  private baseUrl: string;

  constructor() {
    // Vite uses import.meta.env instead of process.env
    this.baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7181';
  }

  /**
   * Creates a Teams meeting
   */
  async createMeeting(request: CreateMeetingRequest): Promise<CreateMeetingResponse> {
    const response = await axios.post<CreateMeetingResponse>(
      `${this.baseUrl}/meeting/create`,
      request
    );
    return response.data;
  }

  /**
   * Gets meeting details
   */
  async getMeeting(eventId: string, organizerEmail: string): Promise<any> {
    const response = await axios.get(
      `${this.baseUrl}/meeting/${eventId}`,
      { params: { organizerEmail } }
    );
    return response.data;
  }

  /**
   * Updates an existing meeting
   */
  async updateMeeting(
    eventId: string,
    updates: Partial<CreateMeetingRequest>
  ): Promise<any> {
    const response = await axios.put(
      `${this.baseUrl}/meeting/${eventId}`,
      updates
    );
    return response.data;
  }

  /**
   * Cancels a meeting
   */
  async cancelMeeting(
    eventId: string,
    organizerEmail: string,
    cancellationMessage?: string
  ): Promise<any> {
    const response = await axios.delete(
      `${this.baseUrl}/meeting/${eventId}`,
      {
        params: {
          organizerEmail,
          cancellationMessage: cancellationMessage || 'Meeting cancelled'
        }
      }
    );
    return response.data;
  }

  /**
   * Sends a message to chat thread
   */
  async sendMessageToThread(
    threadId: string,
    userId: string,
    displayName: string,
    message: string
  ): Promise<any> {
    const response = await axios.post(
      `${this.baseUrl}/chat/message/send`,
      {
        userId,
        displayName,
        threadId,
        message,
        messageType: 'text'
      }
    );
    return response.data;
  }
}

const meetingService = new MeetingService();

// ============================================================================
// 1. Meeting Scheduler Component
// ============================================================================

export function MeetingScheduler({ chatSession, onMeetingCreated }: MeetingSchedulerProps) {
  const { showSuccess, showError, showInfo } = useNotifications();
  const [isScheduling, setIsScheduling] = useState(false);
  const [showScheduler, setShowScheduler] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [subject, setSubject] = useState('Customer Support Call');
  const [description, setDescription] = useState('');
  const [startTime, setStartTime] = useState(getDefaultStartTime());
  const [duration, setDuration] = useState(30); // minutes

  function getDefaultStartTime(): string {
    // Default: 5 minutes from now
    const date = new Date();
    date.setMinutes(date.getMinutes() + 5);
    // Format for datetime-local input: YYYY-MM-DDTHH:mm
    return date.toISOString().slice(0, 16);
  }

  function calculateEndTime(start: string, durationMin: number): string {
    const startDate = new Date(start);
    const endDate = new Date(startDate.getTime() + durationMin * 60000);
    return endDate.toISOString();
  }

  async function handleQuickSchedule() {
    // Quick schedule: 5 min from now, 30 min duration
    await createMeeting({
      subject: 'Customer Support Call',
      description: `Support call for ${chatSession.customerName}`,
      startDateTime: getDefaultStartTime(),
      duration: 30
    });
  }

  async function handleCustomSchedule() {
    await createMeeting({
      subject,
      description,
      startDateTime: startTime,
      duration
    });
  }

  async function createMeeting(options: {
    subject: string;
    description: string;
    startDateTime: string;
    duration: number;
  }) {
    setIsScheduling(true);
    setError(null);

    // Show info notification
    showInfo('📅 Creating Meeting...', 'Setting up Teams meeting and sending invitation...');

    try {
      console.log('[Agent] Creating meeting...');

      const request: CreateMeetingRequest = {
        threadId: chatSession.threadId,
        customerName: chatSession.customerName,
        customerEmail: chatSession.customerEmail,
        agentEmail: chatSession.agentEmail,
        startDateTime: new Date(options.startDateTime).toISOString(),
        endDateTime: calculateEndTime(options.startDateTime, options.duration),
        subject: options.subject,
        description: options.description,
        timeZone: 'UTC'
      };

      // Create the meeting
      const meeting = await meetingService.createMeeting(request);

      if (!meeting.success) {
        throw new Error(meeting.errorMessage || 'Failed to create meeting');
      }

      console.log('[Agent] Meeting created successfully:', meeting.eventId);

      // Send meeting link to customer
      await sendMeetingLinkToCustomer(meeting);

      // Notify parent component
      onMeetingCreated(meeting);

      // Close scheduler
      setShowScheduler(false);

      // Show success notification
      showSuccess(
        '✅ Meeting Created!',
        `Teams meeting scheduled with ${chatSession.customerName}. Invitation sent via chat.`
      );

    } catch (err) {
      console.error('[Agent] Failed to create meeting:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to create meeting';
      setError(errorMessage);
      showError(
        '❌ Meeting Creation Failed',
        errorMessage
      );
    } finally {
      setIsScheduling(false);
    }
  }

  async function sendMeetingLinkToCustomer(meeting: CreateMeetingResponse) {
    try {
      console.log('[Agent] Sending meeting link to customer...');

      const startDate = new Date(meeting.startDateTime);
      const formattedStart = startDate.toLocaleString('en-US', {
        dateStyle: 'full',
        timeStyle: 'short'
      });

      const message = `📅 **Teams Meeting Scheduled**\n\n` +
        `I've scheduled a meeting for us!\n\n` +
        `**Time:** ${formattedStart}\n` +
        `**Duration:** 30 minutes\n\n` +
        `Click the link below to join:\n${meeting.joinUrl}\n\n` +
        `See you there! 👋`;

      await meetingService.sendMessageToThread(
        chatSession.threadId,
        chatSession.agentId,
        chatSession.agentName,
        message
      );

      console.log('[Agent] Meeting invitation sent to customer ✓');
    } catch (err) {
      console.error('[Agent] Failed to send meeting link:', err);
      // Don't throw - meeting was created successfully
    }
  }

  if (showScheduler) {
    return (
      <div className="meeting-scheduler">
        <div className="scheduler-header">
          <h3>Schedule Teams Meeting</h3>
          <button
            className="btn-close"
            onClick={() => setShowScheduler(false)}
          >
            ✕
          </button>
        </div>

        <div className="scheduler-body">
          {error && (
            <div className="error-message">
              ❌ {error}
            </div>
          )}

          <div className="form-group">
            <label>Meeting Subject</label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="Customer Support Call"
            />
          </div>

          <div className="form-group">
            <label>Description (Optional)</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Add meeting notes..."
              rows={3}
            />
          </div>

          <div className="form-group">
            <label>Start Time</label>
            <input
              type="datetime-local"
              value={startTime}
              onChange={(e) => setStartTime(e.target.value)}
            />
          </div>

          <div className="form-group">
            <label>Duration</label>
            <select
              value={duration}
              onChange={(e) => setDuration(parseInt(e.target.value))}
            >
              <option value={15}>15 minutes</option>
              <option value={30}>30 minutes</option>
              <option value={45}>45 minutes</option>
              <option value={60}>1 hour</option>
            </select>
          </div>
        </div>

        <div className="scheduler-footer">
          <button
            className="btn-cancel"
            onClick={() => setShowScheduler(false)}
            disabled={isScheduling}
          >
            Cancel
          </button>
          <button
            className="btn-schedule"
            onClick={handleCustomSchedule}
            disabled={isScheduling}
          >
            {isScheduling ? 'Creating...' : 'Schedule Meeting'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="meeting-actions">
      <button
        className="btn-quick-meeting"
        onClick={handleQuickSchedule}
        disabled={isScheduling}
        title="Schedule a meeting starting in 5 minutes"
      >
        {isScheduling ? '⏳ Creating Meeting...' : '📅 Quick Meeting (5 min)'}
      </button>
      <button
        className="btn-custom-meeting"
        onClick={() => setShowScheduler(true)}
        disabled={isScheduling}
        title="Schedule a meeting with custom time"
      >
        📅 Schedule Meeting
      </button>
    </div>
  );
}

// ============================================================================
// 2. Meeting Controls Component
// ============================================================================

export function MeetingControls({ meeting, onMeetingCancelled }: MeetingControlsProps) {
  const { showSuccess, showError, showWarning } = useNotifications();
  const [isCancelling, setIsCancelling] = useState(false);

  async function handleCancelMeeting() {
    // Show warning confirmation
    showWarning(
      '⚠️ Cancel Meeting?',
      'This will cancel the meeting and notify the customer. This action cannot be undone.'
    );
    
    if (!confirm('Are you sure you want to cancel this meeting?')) {
      return;
    }

    setIsCancelling(true);

    try {
      console.log('[Agent] Cancelling meeting...');

      const agentEmail = meeting.threadId; // Get from context
      await meetingService.cancelMeeting(
        meeting.eventId,
        agentEmail,
        'This meeting has been cancelled. Please contact support if you have questions.'
      );

      console.log('[Agent] Meeting cancelled successfully');
      onMeetingCancelled();

      showSuccess(
        '✅ Meeting Cancelled',
        'The meeting has been cancelled and the customer has been notified.'
      );
    } catch (err) {
      console.error('[Agent] Failed to cancel meeting:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to cancel meeting';
      showError(
        '❌ Cancellation Failed',
        errorMessage
      );
    } finally {
      setIsCancelling(false);
    }
  }

  const startDate = new Date(meeting.startDateTime);
  const now = new Date();
  const isPast = startDate < now;
  const isNow = Math.abs(startDate.getTime() - now.getTime()) < 5 * 60000; // Within 5 minutes

  return (
    <div className="meeting-info">
      <div className="meeting-status">
        {isPast && <span className="status-badge past">Completed</span>}
        {isNow && <span className="status-badge active">Starting Soon</span>}
        {!isPast && !isNow && <span className="status-badge scheduled">Scheduled</span>}
      </div>

      <div className="meeting-details">
        <div className="detail-row">
          <span className="label">Start Time:</span>
          <span className="value">{startDate.toLocaleString()}</span>
        </div>
        <div className="detail-row">
          <span className="label">Meeting Link:</span>
          <a href={meeting.joinUrl} target="_blank" rel="noopener noreferrer">
            Join Meeting
          </a>
        </div>
      </div>

      {!isPast && (
        <div className="meeting-controls">
          <button
            className="btn-cancel-meeting"
            onClick={handleCancelMeeting}
            disabled={isCancelling}
          >
            {isCancelling ? 'Cancelling...' : 'Cancel Meeting'}
          </button>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// 3. Complete Agent Chat Interface
// ============================================================================

export function AgentChatInterface() {
  const [activeMeeting, setActiveMeeting] = useState<CreateMeetingResponse | null>(null);

  // Mock chat session - replace with actual data
  const chatSession: AgentChatSession = {
    threadId: '19:acsV2_...',
    customerId: '8:acs:...',
    customerName: 'John Customer',
    customerEmail: 'customer@example.com',
    agentId: '8:acs:...',
    agentName: 'Support Agent',
    agentEmail: 'agent@office365clinic.com'
  };

  function handleMeetingCreated(meeting: CreateMeetingResponse) {
    console.log('[Agent] Meeting created:', meeting);
    setActiveMeeting(meeting);
  }

  function handleMeetingCancelled() {
    console.log('[Agent] Meeting cancelled');
    setActiveMeeting(null);
  }

  return (
    <div className="agent-chat-container">
      {/* Chat Header */}
      <div className="chat-header">
        <h2>Chat with {chatSession.customerName}</h2>
      </div>

      {/* Chat Messages */}
      <div className="chat-messages">
        {/* Messages go here */}
      </div>

      {/* Meeting Section */}
      <div className="meeting-section">
        {activeMeeting ? (
          <MeetingControls
            meeting={activeMeeting}
            onMeetingCancelled={handleMeetingCancelled}
          />
        ) : (
          <MeetingScheduler
            chatSession={chatSession}
            onMeetingCreated={handleMeetingCreated}
          />
        )}
      </div>

      {/* Chat Input */}
      <div className="chat-input">
        {/* Message input goes here */}
      </div>
    </div>
  );
}

// ============================================================================
// 4. CSS Styles (Add to your global styles or module.css)
// ============================================================================

export const agentMeetingStyles = `
.meeting-actions {
  display: flex;
  gap: 8px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 4px;
  margin: 12px 0;
}

.btn-quick-meeting,
.btn-custom-meeting {
  flex: 1;
  padding: 10px 16px;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-quick-meeting {
  background: #0078d4;
  color: white;
}

.btn-quick-meeting:hover {
  background: #006cbd;
}

.btn-custom-meeting {
  background: white;
  color: #0078d4;
  border: 1px solid #0078d4;
}

.btn-custom-meeting:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.meeting-scheduler {
  position: fixed;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);
  width: 500px;
  max-width: 90vw;
  z-index: 1000;
}

.scheduler-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
}

.scheduler-header h3 {
  margin: 0;
  font-size: 20px;
}

.btn-close {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
}

.scheduler-body {
  padding: 20px;
}

.error-message {
  background: #fef0f0;
  color: #d13438;
  padding: 12px;
  border-radius: 4px;
  margin-bottom: 16px;
}

.form-group {
  margin-bottom: 16px;
}

.form-group label {
  display: block;
  margin-bottom: 6px;
  font-weight: 500;
  color: #333;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 14px;
}

.form-group textarea {
  resize: vertical;
  font-family: inherit;
}

.scheduler-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 20px;
  border-top: 1px solid #e0e0e0;
}

.btn-cancel,
.btn-schedule {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
}

.btn-cancel {
  background: #f5f5f5;
  color: #333;
}

.btn-schedule {
  background: #0078d4;
  color: white;
}

.btn-schedule:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.meeting-info {
  background: #f5f5f5;
  border-radius: 8px;
  padding: 16px;
  margin: 12px 0;
}

.meeting-status {
  margin-bottom: 12px;
}

.status-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.status-badge.scheduled {
  background: #e3f2fd;
  color: #0078d4;
}

.status-badge.active {
  background: #fff3e0;
  color: #f57c00;
}

.status-badge.past {
  background: #f5f5f5;
  color: #666;
}

.meeting-details {
  margin: 12px 0;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid #e0e0e0;
}

.detail-row:last-child {
  border-bottom: none;
}

.detail-row .label {
  font-weight: 500;
  color: #666;
}

.detail-row a {
  color: #0078d4;
  text-decoration: none;
}

.detail-row a:hover {
  text-decoration: underline;
}

.meeting-controls {
  margin-top: 12px;
}

.btn-cancel-meeting {
  padding: 8px 16px;
  background: #d13438;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
}

.btn-cancel-meeting:hover {
  background: #a52729;
}

.btn-cancel-meeting:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
`;
