import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export interface SignalRConnection {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  error: string | null;
}

export const useSignalR = (apiBaseUrl: string) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!apiBaseUrl) return;

    const connectToSignalR = async () => {
      try {
        console.log('[SignalR] Connecting to:', `${apiBaseUrl}/api`);

        const newConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${apiBaseUrl}/api`)
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Information)
          .build();

        // Handle reconnection events
        newConnection.onreconnecting((error) => {
          console.warn('[SignalR] Connection lost. Reconnecting...', error);
          setIsConnected(false);
        });

        newConnection.onreconnected((connectionId) => {
          console.log('[SignalR] Reconnected. Connection ID:', connectionId);
          setIsConnected(true);
        });

        newConnection.onclose((error) => {
          console.error('[SignalR] Connection closed', error);
          setIsConnected(false);
          setError(error?.message || 'Connection closed');
        });

        await newConnection.start();
        console.log('[SignalR] Connected successfully');
        setConnection(newConnection);
        setIsConnected(true);
        setError(null);
      } catch (err: any) {
        console.error('[SignalR] Connection failed:', err);
        setError(err.message || 'Failed to connect');
        setIsConnected(false);
      }
    };

    connectToSignalR();

    return () => {
      if (connection) {
        console.log('[SignalR] Disconnecting...');
        connection.stop();
      }
    };
  }, [apiBaseUrl]);

  const subscribe = useCallback(
    (eventName: string, handler: (...args: any[]) => void) => {
      if (connection) {
        connection.on(eventName, handler);
        console.log(`[SignalR] Subscribed to event: ${eventName}`);
      }
    },
    [connection]
  );

  const unsubscribe = useCallback(
    (eventName: string, handler: (...args: any[]) => void) => {
      if (connection) {
        connection.off(eventName, handler);
        console.log(`[SignalR] Unsubscribed from event: ${eventName}`);
      }
    },
    [connection]
  );

  return {
    connection,
    isConnected,
    error,
    subscribe,
    unsubscribe,
  };
};
