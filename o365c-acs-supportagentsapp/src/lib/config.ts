/// <reference types="vite/client" />

const config = {
  clientId: import.meta.env.VITE_CLIENT_ID,
  tenantId: import.meta.env.VITE_TENANT_ID,
  initiateLoginEndpoint: `${window.location.origin}/auth-start.html`,
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'https://d1f22719ca0d.ngrok-free.app/api',
  signalRApiBaseUrl: import.meta.env.VITE_SIGNALR_API_BASE_URL || 'https://functionapp-acs-signalr.azurewebsites.net',
};

export const getApiBaseUrl = () => config.apiBaseUrl;
export const getSignalRApiBaseUrl = () => config.signalRApiBaseUrl;

export default config;
