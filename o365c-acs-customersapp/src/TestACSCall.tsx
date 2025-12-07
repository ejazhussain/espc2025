import React, { useMemo } from 'react';
import {
  CallWithChatComposite,
  useAzureCommunicationCallWithChatAdapter,
  FluentThemeProvider
} from '@azure/communication-react';
import { AzureCommunicationTokenCredential, CommunicationUserIdentifier } from '@azure/communication-common';
import { TeamsMeetingLinkLocator } from '@azure/communication-calling';
import { Spinner, initializeIcons, Theme, PartialTheme } from '@fluentui/react';

// Initialize Fluent UI icons
initializeIcons();

/**
 * Simple Test Component for ACS CallWithChatComposite
 * Using actual test data
 */
export const TestACSCall = (): JSX.Element => {
  // Actual test values
  const testToken = 'eyJhbGciOiJSUzI1NiIsImtpZCI6IjAxOUQzMTYyMzQ0RTQ4REEwNUU1OUQxMzYwNkYwQkFDRjU4QTQwRUMiLCJ4NXQiOiJBWjB4WWpST1NOb0Y1WjBUWUc4THJQV0tRT3ciLCJ0eXAiOiJKV1QifQ.eyJza3lwZWlkIjoiYWNzOjlkYmQ5YWVjLTEyMzAtNGRkYi04OWMzLTRlOWEzNTQwYmUyOV8wMDAwMDAyYi0yZDQ2LTA2NWMtMTkxNC00YzNhMGQwMDRiZjciLCJzY3AiOjE3OTIsImNzaSI6IjE3NjMzMDg3NTciLCJleHAiOjE3NjMzOTUxNTcsInJnbiI6InVrIiwiYWNzU2NvcGUiOiJjaGF0LHZvaXAiLCJyZXNvdXJjZUlkIjoiOWRiZDlhZWMtMTIzMC00ZGRiLTg5YzMtNGU5YTM1NDBiZTI5IiwicmVzb3VyY2VMb2NhdGlvbiI6InVrIiwiaWF0IjoxNzYzMzA4NzU3fQ.j2pRbh634dxz_9NSBzklWaUG6-iyDbwOPIQulE2rBQQG35R1YGaDoMeRIqRRGNWjVjr6OqR35PPGW0l00Xpk0ngijQW0_JJirlDwZWrb_ZrR-lUlZ6faCS1vxIoBdv-ZadpcPvU35xkk02tuqYEuK3Mm5NtpWDy1IqOyevZfcsaQ9uSJ4CwlcnX63zYRuGlqtvskwGCo69YVAjBvChDHNK7i4MrvfiwoKmrPjF0j2OpXWy4J35indKV_Zq3A30EkKCVYxgC8npjhhbcIJux2y21g_m-4z05HwdOhfZ0CAvbMoivov9Xzp6vu9ywVin14T-oIhzBAcjUt6jlgnWg4LQ';
  const testUserId = '8:acs:9dbd9aec-1230-4ddb-89c3-4e9a3540be29_0000002b-2d46-065c-1914-4c3a0d004bf7';
  const testDisplayName = 'Test User';
  const testMeetingLink = 'https://teams.microsoft.com/l/meetup-join/19%3ameeting_YTgzZGM3ZWUtNmM1MC00MWEyLWE0YTUtNGJkODAyNzc4MjE0%40thread.v2/0?context=%7b%22Tid%22%3a%223f4d536c-9ebc-4eb1-8304-0f0f2f840b5d%22%2c%22Oid%22%3a%222a5de346-1d63-4c7a-897f-b1f4b5316fe5%22%7d';
  const testEndpoint = 'https://acs-customer-agent.uk.communication.azure.com';

  // Construct credential - must be memoized
  const credential = useMemo(
    () => new AzureCommunicationTokenCredential(testToken),
    [testToken]
  );

  // Construct userId - must be memoized
  const communicationUserId: CommunicationUserIdentifier = useMemo(
    () => ({ communicationUserId: testUserId }),
    [testUserId]
  );

  // Construct meeting locator - must be memoized
  const locator: TeamsMeetingLinkLocator = useMemo(
    () => ({ meetingLink: decodeURIComponent(testMeetingLink) }),
    [testMeetingLink]
  );

  // Create adapter using the official hook
  const adapter = useAzureCommunicationCallWithChatAdapter({
    userId: communicationUserId,
    displayName: testDisplayName,
    credential,
    locator,
    endpoint: testEndpoint
  });

  // Show loading while adapter is being created
  if (!adapter) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontSize: '18px'
      }}>
        Loading ACS Composite...
      </div>
    );
  }

  // Render the composite - NO fluentTheme prop, FluentThemeProvider handles theming
  return (
    <FluentThemeProvider>
      <div style={{ width: '100vw', height: '100vh' }}>
        <CallWithChatComposite
          adapter={adapter}
          formFactor="desktop"
        />
      </div>
    </FluentThemeProvider>
  );
};
