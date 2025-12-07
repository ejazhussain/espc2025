import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { AuthenticationResult } from '@azure/msal-browser';
import { msalWrapper } from './MSALWrapper';
import { User, AuthContextType, AuthError } from './types';

// Create authentication context
const AuthContext = createContext<AuthContextType | undefined>(undefined);

/**
 * Authentication Provider Component
 * Manages authentication state and provides MSAL functionality
 * 
 * Features:
 * - Automatic initialization and account detection
 * - Error handling with retry capability
 * - Loading states for better UX
 * - Secure token management
 * 
 * Follows React best practices for context management
 */
export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [user, setUser] = useState<User | null>(null);
    const [error, setError] = useState<string | null>(null);

    // Initialize MSAL and check for existing authentication
    useEffect(() => {
        const initAuth = async () => {
            try {
                setIsLoading(true);
                setError(null);

                // Initialize MSAL wrapper
                await msalWrapper.initialize();
                
                // Handle redirect response (if any)
                const response = await msalWrapper.handleRedirectPromise();
                if (response) {
                    handleAuthResponse(response);
                } else {
                    // Check for existing accounts in cache
                    const accounts = msalWrapper.getAllAccounts();
                    if (accounts.length > 0) {
                        const account = accounts[0];
                        const userData = msalWrapper.extractUserFromAccount(account);
                        setUser(userData);
                        setIsAuthenticated(true);
                        console.log('[Auth Provider] User already authenticated:', userData.displayName);
                    }
                }
            } catch (err) {
                console.error('[Auth Provider] Initialization failed:', err);
                
                if (err instanceof AuthError) {
                    setError(`Authentication failed: ${err.message}`);
                } else {
                    setError(`Unexpected authentication error: ${err}`);
                }
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    /**
     * Handle successful authentication response
     * Extracts user data from Azure AD token claims
     */
    const handleAuthResponse = (response: AuthenticationResult): void => {
        try {
            const account = response.account;
            if (account) {
                const userData = msalWrapper.extractUserFromAccount(account);
                setUser(userData);
                setIsAuthenticated(true);
                setError(null);
                console.log('[Auth Provider] Authentication successful:', userData.displayName);
            }
        } catch (err) {
            console.error('[Auth Provider] Error processing auth response:', err);
            setError('Failed to process authentication response');
        }
    };

    /**
     * Login function - uses popup flow for better UX
     * Implements proper error handling and loading states
     */
    const login = async (): Promise<void> => {
        try {
            setIsLoading(true);
            setError(null);
            
            const response = await msalWrapper.loginPopup();
            handleAuthResponse(response);
        } catch (err) {
            console.error('[Auth Provider] Login failed:', err);
            
            if (err instanceof AuthError) {
                setError(`Login failed: ${err.message}`);
            } else {
                setError(`Login failed: ${err}`);
            }
        } finally {
            setIsLoading(false);
        }
    };

    /**
     * Logout function - clears tokens and user state
     * Uses popup flow to maintain application state
     */
    const logout = async (): Promise<void> => {
        try {
            setIsLoading(true);
            setError(null);
            
            await msalWrapper.logoutPopup();
            
            // Clear application state
            setIsAuthenticated(false);
            setUser(null);
            console.log('[Auth Provider] Logout successful');
        } catch (err) {
            console.error('[Auth Provider] Logout failed:', err);
            
            if (err instanceof AuthError) {
                setError(`Logout failed: ${err.message}`);
            } else {
                setError(`Logout failed: ${err}`);
            }
        } finally {
            setIsLoading(false);
        }
    };

    /**
     * Clear error state
     * Allows users to retry after fixing issues
     */
    const clearError = (): void => {
        setError(null);
    };

    // Context value with all authentication state and methods
    const contextValue: AuthContextType = {
        isAuthenticated,
        isLoading,
        user,
        error,
        login,
        logout,
        clearError,
    };

    return (
        <AuthContext.Provider value={contextValue}>
            {children}
        </AuthContext.Provider>
    );
};

/**
 * Custom hook to use authentication context
 * Provides type-safe access to authentication state and methods
 * 
 * Usage:
 * const { isAuthenticated, user, login, logout } = useAuth();
 */
export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};