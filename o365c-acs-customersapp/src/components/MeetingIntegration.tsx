/**
 * Teams Meeting Integration - Customer App
 * 
 * This component provides the complete meeting integration for customers:
 * - Detects Teams meeting links in chat messages
 * - Provides "Join Meeting" button
 * - Full video calling interface with ACS Calling SDK
 * - Audio/video controls (mute, camera, hang up)
 */

import React, { useState, useEffect, useRef } from 'react';
import {
  CallClient,
  CallAgent,
  Call,
  DeviceManager,
  VideoStreamRenderer,
  LocalVideoStream,
  RemoteParticipant,
  RemoteVideoStream,
} from '@azure/communication-calling';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import axios from 'axios';

// ============================================================================
// Types & Interfaces
// ============================================================================

interface MeetingLinkMessageProps {
  message: {
    id: string;
    content: string;
    senderId: string;
    senderDisplayName: string;
    createdOn: Date;
  };
  threadId: string;
  userId: string;
  onJoinMeeting: (meetingLink: string) => void;
}

interface MeetingCallProps {
  meetingLink: string;
  threadId: string;
  userId: string;
  displayName: string;
  onCallEnded: () => void;
}

interface MeetingValidationResponse {
  canAccess: boolean;
  meetingInfo?: {
    meetingLink: string;
    meetingId: string;
    threadId: string;
  };
}

interface MeetingTokenResponse {
  success: boolean;
  token: string;
  userId: string;
  expiresOn: string;
}

// ============================================================================
// 1. Meeting Link Detection Component
// ============================================================================

export function MeetingLinkMessage({
  message,
  threadId,
  userId,
  onJoinMeeting
}: MeetingLinkMessageProps) {
  const [isValidating, setIsValidating] = useState(false);
  const [canJoin, setCanJoin] = useState(false);

  // Regex to detect Teams meeting links
  // Extract Teams meeting link from message content
  // Updated regex to stop at quotes or whitespace to avoid capturing trailing characters
  const teamsLinkRegex = /https:\/\/teams\.microsoft\.com\/l\/meetup-join\/[^"\s]+/g;
  const meetingLink = message.content.match(teamsLinkRegex)?.[0];

  useEffect(() => {
    if (meetingLink) {
      validateMeetingAccess();
    }
  }, [meetingLink]);

  async function validateMeetingAccess() {
    setIsValidating(true);
    console.log('[MeetingLinkMessage] Validating meeting access...', {
      threadId,
      userId,
      meetingLink: meetingLink?.substring(0, 50) + '...'
    });
    
    try {
      const apiBaseUrl = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7181';
      console.log('[MeetingLinkMessage] API Base URL:', apiBaseUrl);
      
      const response = await axios.post<MeetingValidationResponse>(
        `${apiBaseUrl}/api/teamsinterop/validate`,
        {
          threadId,
          userId
        }
      );

      console.log('[MeetingLinkMessage] Validation response:', response.data);
      setCanJoin(response.data.canAccess);
      
      if (!response.data.canAccess) {
        console.warn('[MeetingLinkMessage] ❌ Cannot join meeting. Reason:', response.data);
      } else {
        console.log('[MeetingLinkMessage] ✅ Can join meeting!');
      }
    } catch (error) {
      console.error('[MeetingLinkMessage] ❌ Validation failed with error:', error);
      if (axios.isAxiosError(error)) {
        console.error('[MeetingLinkMessage] Response data:', error.response?.data);
        console.error('[MeetingLinkMessage] Response status:', error.response?.status);
        
        // If endpoint doesn't exist (404), allow joining anyway for demo purposes
        if (error.response?.status === 404) {
          console.warn('[MeetingLinkMessage] ⚠️ Validation endpoint not found (404). Allowing join for demo purposes.');
          setCanJoin(true);
          return;
        }
      }
      setCanJoin(false);
    } finally {
      setIsValidating(false);
    }
  }

  if (!meetingLink) {
    return null;
  }

  return (
    <div className="meeting-invitation">
      <div className="meeting-icon">🎥</div>
      <div className="meeting-content">
        <h4>Teams Meeting Invitation</h4>
        <p>Click below to join the video call</p>
        {isValidating ? (
          <button className="btn-join-meeting" disabled style={{ color: '#ffffff', backgroundColor: '#94a3b8' }}>
            Validating...
          </button>
        ) : canJoin ? (
          <button
            className="btn-join-meeting"
            onClick={() => onJoinMeeting(meetingLink)}
            style={{ color: '#ffffff', backgroundColor: '#3b82f6' }}
          >
            Join Meeting
          </button>
        ) : (
          <div className="meeting-error">
            Unable to join this meeting
          </div>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// 2. Meeting Call Component (Full Video UI)
// ============================================================================

export function MeetingCall({
  meetingLink,
  threadId,
  userId,
  displayName,
  onCallEnded
}: MeetingCallProps) {
  // State management
  const [callClient, setCallClient] = useState<CallClient | null>(null);
  const [callAgent, setCallAgent] = useState<CallAgent | null>(null);
  const [deviceManager, setDeviceManager] = useState<DeviceManager | null>(null);
  const [call, setCall] = useState<Call | null>(null);
  const [callState, setCallState] = useState<string>('Initializing');
  const [isMuted, setIsMuted] = useState(false);
  const [isVideoOn, setIsVideoOn] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [localVideoStream, setLocalVideoStream] = useState<LocalVideoStream | null>(null);
  const [remoteParticipants, setRemoteParticipants] = useState<RemoteParticipant[]>([]);

  // Refs for video elements
  const localVideoRef = useRef<HTMLDivElement>(null);
  const remoteVideoRef = useRef<HTMLDivElement>(null);

  // Initialize meeting on mount
  useEffect(() => {
    initializeMeeting();

    return () => {
      cleanup();
    };
  }, []);

  // ========================================================================
  // Initialize ACS SDK and Join Meeting
  // ========================================================================

  async function initializeMeeting() {
    try {
      console.log('[Meeting] Initializing...');

      // Step 1: Validate access
      const apiBaseUrl = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7181';
      
      console.log('[Meeting] Step 1.1: Validating access...');
      const validationResponse = await axios.post<MeetingValidationResponse>(
        `${apiBaseUrl}/api/teamsinterop/validate`,
        { threadId, userId }
      );

      if (!validationResponse.data.canAccess) {
        throw new Error('You do not have access to this meeting');
      }

      // Step 2: Get ACS token
      console.log('[Meeting] Step 1.2: Getting meeting token...');
      const tokenResponse = await axios.post<MeetingTokenResponse>(
        `${apiBaseUrl}/api/teamsinterop/token`,
        { threadId, userId, displayName }
      );

      if (!tokenResponse.data.success) {
        throw new Error('Failed to get meeting token');
      }

      // Step 3: Initialize CallClient
      console.log('[Meeting] Step 1.3: Initializing CallClient...');
      const client = new CallClient();
      setCallClient(client);

      // Step 4: Create CallAgent
      console.log('[Meeting] Step 1.4: Creating CallAgent...');
      const tokenCredential = new AzureCommunicationTokenCredential(tokenResponse.data.token);
      const agent = await client.createCallAgent(tokenCredential, { displayName });
      setCallAgent(agent);

      // Step 5: Get DeviceManager
      console.log('[Meeting] Step 1.5: Getting DeviceManager...');
      const devManager = await client.getDeviceManager();
      setDeviceManager(devManager);

      // Step 6: Request permissions
      console.log('[Meeting] Step 1.6: Requesting permissions...');
      await devManager.askDevicePermission({ video: true, audio: true });

      // Step 7: Join meeting
      console.log('[Meeting] Step 1.7: Joining meeting...');
      await joinMeeting(agent, devManager);

    } catch (err) {
      console.error('[Meeting] Initialization failed:', err);
      setError(err instanceof Error ? err.message : 'Failed to initialize meeting');
      setCallState('Error');
    }
  }

  async function joinMeeting(agent: CallAgent, devManager: DeviceManager) {
    try {
      console.log('[Meeting] Join meeting with link:', meetingLink);
      console.log('[Meeting] Meeting link length:', meetingLink.length);
      console.log('[Meeting] Meeting link format check:', {
        startsWith: meetingLink.substring(0, 50),
        isTeamsLink: meetingLink.includes('teams.microsoft.com')
      });

      // Decode URL-encoded meeting link (ACS SDK requires decoded URLs)
      const decodedMeetingLink = decodeURIComponent(meetingLink);
      console.log('[Meeting] Decoded meeting link:', decodedMeetingLink);

      // Get available cameras
      const cameras = await devManager.getCameras();
      if (cameras.length === 0) {
        console.warn('[Meeting] No cameras found');
      }

      // Create local video stream
      let localStream: LocalVideoStream | null = null;
      if (cameras.length > 0) {
        localStream = new LocalVideoStream(cameras[0]);
        setLocalVideoStream(localStream);
      }

      // Join the meeting with decoded link
      const joinOptions = localStream ? { videoOptions: { localVideoStreams: [localStream] } } : {};
      const currentCall = agent.join({ meetingLink: decodedMeetingLink }, joinOptions);
      setCall(currentCall);

      // Setup call event handlers
      setupCallEventHandlers(currentCall);

      setCallState('Connecting');
      console.log('[Meeting] Joined meeting successfully');

    } catch (err) {
      console.error('[Meeting] Failed to join meeting:', err);
      throw err;
    }
  }

  // ========================================================================
  // Event Handlers
  // ========================================================================

  function setupCallEventHandlers(currentCall: Call) {
    // Call state changed
    currentCall.on('stateChanged', () => {
      const state = currentCall.state;
      console.log('[Meeting] Call state:', state);
      setCallState(state);

      if (state === 'Disconnected') {
        handleCallEnded();
      }
    });

    // Remote participants updated
    currentCall.on('remoteParticipantsUpdated', (e: any) => {
      console.log('[Meeting] Remote participants updated');
      
      // Handle added participants
      e.added.forEach((participant: RemoteParticipant) => {
        console.log('[Meeting] Participant joined:', participant.displayName);
        subscribeToParticipant(participant);
      });

      // Update participants list
      setRemoteParticipants(Array.from(currentCall.remoteParticipants.values()));
    });

    // Subscribe to existing participants
    currentCall.remoteParticipants.forEach(subscribeToParticipant);
  }

  function subscribeToParticipant(participant: RemoteParticipant) {
    // Subscribe to video streams
    participant.on('videoStreamsUpdated', (e: any) => {
      e.added.forEach((stream: RemoteVideoStream) => {
        console.log('[Meeting] Remote video stream added');
        renderRemoteVideo(stream);
      });
    });

    // Render existing video streams
    participant.videoStreams.forEach((stream) => {
      if (stream.isAvailable) {
        renderRemoteVideo(stream);
      }
    });
  }

  // ========================================================================
  // Video Rendering
  // ========================================================================

  async function renderLocalVideo() {
    if (!localVideoStream || !localVideoRef.current) return;

    try {
      const renderer = new VideoStreamRenderer(localVideoStream);
      const view = await renderer.createView();
      localVideoRef.current.innerHTML = '';
      localVideoRef.current.appendChild(view.target);
    } catch (err) {
      console.error('[Meeting] Failed to render local video:', err);
    }
  }

  async function renderRemoteVideo(stream: RemoteVideoStream) {
    if (!remoteVideoRef.current) return;

    try {
      const renderer = new VideoStreamRenderer(stream);
      const view = await renderer.createView();
      remoteVideoRef.current.innerHTML = '';
      remoteVideoRef.current.appendChild(view.target);
    } catch (err) {
      console.error('[Meeting] Failed to render remote video:', err);
    }
  }

  // Render local video when stream is available
  useEffect(() => {
    if (localVideoStream && isVideoOn) {
      renderLocalVideo();
    }
  }, [localVideoStream, isVideoOn]);

  // ========================================================================
  // Call Controls
  // ========================================================================

  async function toggleMute() {
    if (!call) return;

    try {
      if (isMuted) {
        await call.unmute();
        setIsMuted(false);
        console.log('[Meeting] Unmuted');
      } else {
        await call.mute();
        setIsMuted(true);
        console.log('[Meeting] Muted');
      }
    } catch (err) {
      console.error('[Meeting] Failed to toggle mute:', err);
    }
  }

  async function toggleVideo() {
    if (!call || !deviceManager) return;

    try {
      if (isVideoOn) {
        // Turn off video
        if (localVideoStream) {
          await call.stopVideo(localVideoStream);
          setIsVideoOn(false);
          console.log('[Meeting] Video off');
        }
      } else {
        // Turn on video
        const cameras = await deviceManager.getCameras();
        if (cameras.length > 0) {
          const stream = new LocalVideoStream(cameras[0]);
          await call.startVideo(stream);
          setLocalVideoStream(stream);
          setIsVideoOn(true);
          console.log('[Meeting] Video on');
        }
      }
    } catch (err) {
      console.error('[Meeting] Failed to toggle video:', err);
    }
  }

  async function hangUp() {
    if (!call) return;

    try {
      await call.hangUp();
      console.log('[Meeting] Hung up');
    } catch (err) {
      console.error('[Meeting] Failed to hang up:', err);
    }
  }

  // ========================================================================
  // Cleanup
  // ========================================================================

  function handleCallEnded() {
    console.log('[Meeting] Call ended');
    cleanup();
    onCallEnded();
  }

  async function cleanup() {
    try {
      if (localVideoStream) {
        localVideoStream.dispose();
      }
      if (callAgent) {
        await callAgent.dispose();
      }
    } catch (err) {
      console.error('[Meeting] Cleanup error:', err);
    }
  }

  // ========================================================================
  // Render UI
  // ========================================================================

  if (callState === 'Initializing') {
    return (
      <div className="meeting-overlay">
        <div className="meeting-loading">
          <div className="spinner"></div>
          <p>Initializing meeting...</p>
        </div>
      </div>
    );
  }

  if (callState === 'Error' || error) {
    return (
      <div className="meeting-overlay">
        <div className="meeting-error-screen">
          <h3>❌ Unable to Join Meeting</h3>
          <p>{error || 'An error occurred'}</p>
          <button onClick={onCallEnded}>Close</button>
        </div>
      </div>
    );
  }

  return (
    <div className="meeting-overlay">
      <div className="meeting-container">
        {/* Remote Video (Main) */}
        <div className="video-grid">
          <div
            ref={remoteVideoRef}
            className="remote-video"
          >
            {remoteParticipants.length === 0 && (
              <div className="no-video-placeholder">
                <div className="avatar-placeholder">
                  {displayName.charAt(0).toUpperCase()}
                </div>
                <p>Waiting for others to join...</p>
              </div>
            )}
          </div>

          {/* Local Video (Picture-in-Picture) */}
          {isVideoOn && (
            <div
              ref={localVideoRef}
              className="local-video"
            />
          )}
        </div>

        {/* Call Controls */}
        <div className="call-controls">
          <button
            className={`control-btn ${isMuted ? 'muted' : ''}`}
            onClick={toggleMute}
            title={isMuted ? 'Unmute' : 'Mute'}
          >
            {isMuted ? '🔇' : '🎤'}
          </button>

          <button
            className={`control-btn ${!isVideoOn ? 'video-off' : ''}`}
            onClick={toggleVideo}
            title={isVideoOn ? 'Turn off camera' : 'Turn on camera'}
          >
            {isVideoOn ? '📹' : '📷'}
          </button>

          <button
            className="control-btn hangup-btn"
            onClick={hangUp}
            title="Hang up"
          >
            📞
          </button>
        </div>

        {/* Call Status */}
        <div className="call-status">
          <span className={`status-indicator ${callState.toLowerCase()}`}>
            {callState}
          </span>
          {remoteParticipants.length > 0 && (
            <span className="participant-count">
              {remoteParticipants.length} participant(s)
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// CSS Styles (Add to your global CSS or component CSS module)
// ============================================================================

export const meetingStyles = `
.meeting-invitation {
  display: flex;
  gap: 12px;
  padding: 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  margin: 8px 0;
  color: white;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.meeting-icon {
  font-size: 32px;
  line-height: 1;
}

.meeting-content {
  flex: 1;
}

.meeting-content h4 {
  margin: 0 0 4px 0;
  font-size: 16px;
  font-weight: 600;
}

.meeting-content p {
  margin: 0 0 12px 0;
  font-size: 14px;
  opacity: 0.9;
}

.btn-join-meeting {
  padding: 10px 20px;
  background: white;
  color: #667eea;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-join-meeting:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

.btn-join-meeting:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.meeting-error {
  padding: 8px 12px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 6px;
  font-size: 14px;
}

.meeting-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: #1a1a1a;
  z-index: 10000;
  display: flex;
  align-items: center;
  justify-content: center;
}

.meeting-loading,
.meeting-error-screen {
  text-align: center;
  color: white;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid rgba(255, 255, 255, 0.1);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 16px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.meeting-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  position: relative;
}

.video-grid {
  flex: 1;
  position: relative;
  background: #000;
}

.remote-video {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.remote-video video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.no-video-placeholder {
  text-align: center;
  color: white;
}

.avatar-placeholder {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  background: #667eea;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 48px;
  font-weight: bold;
  margin: 0 auto 16px;
}

.local-video {
  position: absolute;
  bottom: 80px;
  right: 20px;
  width: 240px;
  height: 180px;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  border: 2px solid white;
}

.local-video video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.call-controls {
  position: absolute;
  bottom: 20px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  gap: 16px;
  padding: 16px;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 50px;
  backdrop-filter: blur(10px);
}

.control-btn {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: none;
  background: rgba(255, 255, 255, 0.2);
  color: white;
  font-size: 24px;
  cursor: pointer;
  transition: all 0.2s;
}

.control-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: scale(1.1);
}

.control-btn.muted,
.control-btn.video-off {
  background: rgba(220, 53, 69, 0.8);
}

.hangup-btn {
  background: #dc3545;
}

.hangup-btn:hover {
  background: #c82333;
}

.call-status {
  position: absolute;
  top: 20px;
  left: 20px;
  display: flex;
  gap: 12px;
  align-items: center;
}

.status-indicator {
  padding: 6px 12px;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  border-radius: 20px;
  font-size: 14px;
  font-weight: 500;
  backdrop-filter: blur(10px);
}

.status-indicator.connected {
  background: rgba(40, 167, 69, 0.8);
}

.participant-count {
  padding: 6px 12px;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  border-radius: 20px;
  font-size: 14px;
  backdrop-filter: blur(10px);
}
`;
