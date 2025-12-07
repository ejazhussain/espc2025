/**
 * Auth module exports
 * Provides clean interface for consuming authentication functionality
 */

// Main authentication hook and provider
export { AuthProvider, useAuth } from './AuthContext';

// MSAL wrapper for advanced scenarios
export { msalWrapper, MSALWrapper } from './MSALWrapper';

// Types for TypeScript consumers
export type { User, AuthContextType } from './types';
export { AuthError, AuthErrorType } from './types';

// Configuration (for testing/debugging only)
export { msalConfig, loginRequest } from './msalConfig';