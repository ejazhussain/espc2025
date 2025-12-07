/**
 * User interface for authenticated user data
 * Contains essential user information from Azure AD tokens
 */
export interface User {
    id: string;
    displayName: string;
    email: string;
    department?: string;
    jobTitle?: string;
}

/**
 * Authentication context interface
 * Defines the contract for authentication state and operations
 */
export interface AuthContextType {
    isAuthenticated: boolean;
    isLoading: boolean;
    user: User | null;
    error: string | null;
    login: () => Promise<void>;
    logout: () => Promise<void>;
    clearError: () => void;
}

/**
 * MSAL error types for better error handling
 */
export enum AuthErrorType {
    INITIALIZATION_ERROR = 'INITIALIZATION_ERROR',
    LOGIN_ERROR = 'LOGIN_ERROR',
    LOGOUT_ERROR = 'LOGOUT_ERROR',
    TOKEN_ERROR = 'TOKEN_ERROR',
}

/**
 * Custom authentication error class
 */
export class AuthError extends Error {
    constructor(
        public type: AuthErrorType,
        message: string,
        public originalError?: any
    ) {
        super(message);
        this.name = 'AuthError';
    }
}