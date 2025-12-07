import { Configuration, LogLevel } from "@azure/msal-browser";

/**
 * MSAL Configuration following Azure security best practices
 * Reference: https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-initializing-client-applications
 * 
 * Security considerations:
 * - Uses localStorage for token caching (secure for SPAs)
 * - Disables PII logging to prevent sensitive data exposure
 * - Minimal logging for production security
 * - Secure cookies only over HTTPS
 */
export const msalConfig: Configuration = {
    auth: {
        clientId: process.env.REACT_APP_CLIENT_ID || "",
        authority: process.env.REACT_APP_AUTHORITY || "https://login.microsoftonline.com/common",
        redirectUri: window.location.origin,
        postLogoutRedirectUri: window.location.origin,
        // Disable automatic redirect for better UX control
        navigateToLoginRequestUrl: false,
    },
    cache: {
        cacheLocation: "localStorage", // Recommended for SPAs
        storeAuthStateInCookie: false, // Set to true for IE11 support if needed
        secureCookies: window.location.protocol === "https:",
    },
    system: {
        loggerOptions: {
            loggerCallback: (level: LogLevel, message: string, containsPii: boolean) => {
                // Never log PII information - critical for security
                if (containsPii) return;
                
                // Only log errors and warnings in production
                if (level === LogLevel.Error) {
                    console.error('[MSAL Error]', message);
                } else if (level === LogLevel.Warning) {
                    console.warn('[MSAL Warning]', message);
                }
                // Debug info only in development
                else if (process.env.NODE_ENV === 'development' && level === LogLevel.Info) {
                    console.info('[MSAL Info]', message);
                }
            },
            logLevel: process.env.NODE_ENV === 'development' ? LogLevel.Info : LogLevel.Error,
            piiLoggingEnabled: false, // Critical: Never enable in production
        },
        // Performance optimization: reduce redirect timeout
        windowHashTimeout: 60000,
        iframeHashTimeout: 6000,
    },
};

/**
 * Login request configuration with Microsoft Graph scopes
 * Minimal scopes for basic user authentication and profile access
 */
export const loginRequest = {
    scopes: [
        "openid",      // Required for authentication
        "profile",     // Basic profile information
        "User.Read",   // Microsoft Graph user profile access
    ],
};

/**
 * Validate environment configuration
 * Ensures required environment variables are set for authentication
 */
export const validateMsalConfig = (): void => {
    if (!process.env.REACT_APP_CLIENT_ID) {
        throw new Error(
            'REACT_APP_CLIENT_ID is required. Please check your .env file and Azure AD app registration.'
        );
    }
    
    // Log configuration status (non-sensitive info only)
    console.log('[MSAL Config] Authentication configured for:', 
        process.env.REACT_APP_AUTHORITY || 'common tenant');
};