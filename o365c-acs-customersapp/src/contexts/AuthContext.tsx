import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { PublicClientApplication, AuthenticationResult } from '@azure/msal-browser';
import { msalConfig, loginRequest } from '../auth/msalConfig';
import { User } from '../types/auth';

interface AuthContextType {
    isAuthenticated: boolean;
    isLoading: boolean;
    user: User | null;
    error: string | null;
    login: () => Promise<void>;
    logout: () => Promise<void>;
    clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Initialize MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [user, setUser] = useState<User | null>(null);
    const [error, setError] = useState<string | null>(null);

    // Initialize MSAL and check for existing authentication
    useEffect(() => {
        const initAuth = async () => {
            try {
                await msalInstance.initialize();
                
                // Handle redirect response
                const response = await msalInstance.handleRedirectPromise();
                if (response) {
                    handleAuthResponse(response);
                } else {
                    // Check for existing accounts
                    const accounts = msalInstance.getAllAccounts();
                    if (accounts.length > 0) {
                        const account = accounts[0];
                        setUser({
                            id: account.homeAccountId,
                            displayName: account.name || account.username,
                            email: account.username,
                            department: (account.idTokenClaims as any)?.department,
                        });
                        setIsAuthenticated(true);
                    }
                }
            } catch (err) {
                setError(`Initialization failed: ${err}`);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    const handleAuthResponse = (response: AuthenticationResult) => {
        const account = response.account;
        if (account) {
            setUser({
                id: account.homeAccountId,
                displayName: account.name || account.username,
                email: account.username,
                department: (account.idTokenClaims as any)?.department,
            });
            setIsAuthenticated(true);
            setError(null);
        }
    };

    const login = async () => {
        try {
            setIsLoading(true);
            setError(null);
            const response = await msalInstance.loginPopup(loginRequest);
            handleAuthResponse(response);
        } catch (err) {
            setError(`Login failed: ${err}`);
        } finally {
            setIsLoading(false);
        }
    };

    const logout = async () => {
        try {
            setIsLoading(true);
            await msalInstance.logoutPopup();
            setIsAuthenticated(false);
            setUser(null);
            setError(null);
        } catch (err) {
            setError(`Logout failed: ${err}`);
        } finally {
            setIsLoading(false);
        }
    };

    const clearError = () => setError(null);

    return (
        <AuthContext.Provider value={{
            isAuthenticated,
            isLoading,
            user,
            error,
            login,
            logout,
            clearError,
        }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider');
    }
    return context;
};