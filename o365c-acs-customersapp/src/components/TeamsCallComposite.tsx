// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useMemo, useState, useEffect } from 'react';
import {
  CallWithChatComposite,
  useAzureCommunicationCallWithChatAdapter,
  CallWithChatCompositeOptions,
  lightTheme
} from '@azure/communication-react';
import { AzureCommunicationTokenCredential, CommunicationUserIdentifier } from '@azure/communication-common';
import { TeamsMeetingLinkLocator } from '@azure/communication-calling';
import { Spinner, initializeIcons } from '@fluentui/react';
import axios from 'axios';
import './TeamsCallComposite.css';

// Initialize Fluent UI icons
initializeIcons();

interface TeamsCallCompositeProps {
  meetingLink: string;
  threadId: string;
  userId: string;
  displayName: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface MeetingTokenResponse {
  success: boolean;
  token?: string;
  userId?: string;
  expiresOn?: string;
  errorMessage?: string;
}

/**
 * Teams Call Experience Component
 * Provides a full Teams-like meeting experience using Azure Communication Services UI Library
 */
const TeamsCallExperience = ({
  token,
  userId,
  displayName,
  meetingLink,
  endpointUrl,
  onCallEnded,
  onCallStateChange
}: {
  token: string;
  userId: string;
  displayName: string;
  meetingLink: string;
  endpointUrl: string;
  onCallEnded: () => void;
  onCallStateChange: (isActive: boolean) => void;
}): JSX.Element => {
  const [callEnded, setCallEnded] = useState(false);
  // Construct credential - must be memoized
  const credential = useMemo(() => new AzureCommunicationTokenCredential(token), [token]);

  // Construct userId as CommunicationUserIdentifier - must be memoized
  const communicationUserId: CommunicationUserIdentifier = useMemo(
    () => ({ communicationUserId: userId }),
    [userId]
  );

  // Construct meeting locator - must be memoized
  const locator: TeamsMeetingLinkLocator = useMemo(
    () => ({ meetingLink: decodeURIComponent(meetingLink) }),
    [meetingLink]
  );

  // Composite options
  const options: CallWithChatCompositeOptions = useMemo(
    () => ({
      callControls: {
        cameraButton: true,
        microphoneButton: true,
        screenShareButton: true,
        devicesButton: true,
        peopleButton: true,
        displayType: 'default',
        endCallButton: true,
        moreButton: true
      },
      localVideoTile: {
        position: 'floating'
      }
    }),
    []
  );

  // Create adapter using the official hook
  const adapter = useAzureCommunicationCallWithChatAdapter({
    userId: communicationUserId,
    displayName,
    credential,
    locator,
    endpoint: endpointUrl
  });

  // Listen to call state changes to detect when call ends
  useEffect(() => {
    if (!adapter) return;

    const onStateChange = () => {
      const callState = adapter.getState().call?.state;
      
      // Notify parent about call active state
      const isActive = callState === 'Connected' || callState === 'Connecting' || 
                       callState === 'Ringing' || callState === 'InLobby';
      onCallStateChange(isActive);
      
      // Check if call has ended
      if (callState === 'Disconnected' || callState === 'Disconnecting') {
        setCallEnded(true);
        onCallStateChange(false);
      }
    };

    adapter.onStateChange(onStateChange);
    return () => {
      adapter.offStateChange(onStateChange);
    };
  }, [adapter, onCallStateChange]);

  // Show spinner while adapter is being created
  if (!adapter) {
    return (
      <div className="flex flex-col items-center justify-center h-full p-10">
        <Spinner label="Initializing Teams meeting..." />
      </div>
    );
  }

  // Show close button when call has ended
  if (callEnded) {
    return (
      <div className="flex flex-col items-center justify-center h-full p-10 gap-4">
        <span className="text-6xl">✓</span>
        <h3 className="text-2xl font-bold text-gray-800">Call Ended</h3>
        <p className="text-gray-600">The meeting has ended.</p>
        <button
          onClick={onCallEnded}
          className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium"
        >
          Close
        </button>
      </div>
    );
  }

  // Render the composite with the adapter
  return (
    <div style={{ width: '100%', height: '100%' }}>
      <CallWithChatComposite
        adapter={adapter}
        formFactor="desktop"
        fluentTheme={lightTheme}
        options={options}
      />
    </div>
  );
};

/**
 * Teams Call Composite Component with Dialog
 * Wraps the Teams experience in a modal dialog
 */
export const TeamsCallComposite = (props: TeamsCallCompositeProps): JSX.Element => {
  const { meetingLink, threadId, userId, displayName, open, onOpenChange } = props;

  const [tokenData, setTokenData] = useState<{ token: string; userId: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoadingToken, setIsLoadingToken] = useState(false);
  const [isCallActive, setIsCallActive] = useState(false);

  const endpointUrl = process.env.REACT_APP_ACS_ENDPOINT_URL || '';
  const apiBaseUrl = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7181';

  // Fetch token when dialog opens
  useEffect(() => {
    if (!open || tokenData) return;

    const fetchToken = async () => {
      setIsLoadingToken(true);
      setError(null);

      try {
        console.log('Fetching meeting token...', { threadId, userId, displayName });

        const response = await axios.post<MeetingTokenResponse>(
          `${apiBaseUrl}/api/teamsinterop/token`,
          {
            threadId,
            userId,
            displayName
          }
        );

        console.log('Token response:', response.data);

        if (response.data.success && response.data.token && response.data.userId) {
          setTokenData({
            token: response.data.token,
            userId: response.data.userId
          });
        } else {
          setError(response.data.errorMessage || 'Failed to get meeting token');
        }
      } catch (err: any) {
        console.error('Failed to fetch meeting token:', err);
        setError(err.response?.data?.errorMessage || err.message || 'Failed to connect to server');
      } finally {
        setIsLoadingToken(false);
      }
    };

    fetchToken();
  }, [open, threadId, userId, displayName, apiBaseUrl, tokenData]);

  // Reset state when dialog closes
  useEffect(() => {
    if (!open) {
      setTokenData(null);
      setError(null);
      setIsLoadingToken(false);
    }
  }, [open]);

  return (
    <>
      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          {/* Overlay - prevent closing if call is active */}
          <div 
            className="fixed inset-0 bg-black/50 backdrop-blur-sm teams-dialog-overlay"
            onClick={() => {
              // Only allow closing if call is not active
              if (!isCallActive) {
                onOpenChange(false);
              }
            }}
          />
          
          {/* Dialog Content */}
          <div className="relative bg-white rounded-lg shadow-2xl w-[95vw] h-[95vh] max-w-[1400px] max-h-[900px] teams-dialog-content">
            {/* Screen reader only titles */}
            <h2 className="sr-only">Teams Meeting</h2>
            <p className="sr-only">Join the Teams meeting to communicate with the support agent</p>

            {isLoadingToken && (
              <div className="flex flex-col items-center justify-center h-full p-10">
                <Spinner label="Getting meeting credentials..." />
              </div>
            )}

            {error && !isLoadingToken && (
              <div className="flex flex-col items-center justify-center gap-4 h-full p-10">
                <span className="text-4xl">❌</span>
                <h3 className="text-xl font-bold text-red-600">
                  Failed to join meeting
                </h3>
                <p className="text-sm text-gray-600 text-center">
                  {error}
                </p>
                <button 
                  onClick={() => onOpenChange(false)}
                  className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
                >
                  Close
                </button>
              </div>
            )}

            {tokenData && !isLoadingToken && !error && endpointUrl && (
              <TeamsCallExperience
                token={tokenData.token}
                userId={tokenData.userId}
                displayName={displayName}
                meetingLink={meetingLink}
                endpointUrl={endpointUrl}
                onCallEnded={() => onOpenChange(false)}
                onCallStateChange={setIsCallActive}
              />
            )}
          </div>
        </div>
      )}
    </>
  );
};
