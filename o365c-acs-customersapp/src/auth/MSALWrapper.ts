import { 
    PublicClientApplication, 
    AuthenticationResult, 
    AccountInfo,
    SilentRequest,
    InteractionRequiredAuthError 
} from '@azure/msal-browser';
import { msalConfig, loginRequest, validateMsalConfig } from './msalConfig';
import { User, AuthError, AuthErrorType } from './types';

/**
 * MSAL Wrapper class - encapsulates authentication operations
 * Implements Azure security best practices for token management
 * 
 * Features:
 * - Singleton pattern for consistent MSAL instance
 * - Proper error handling with custom error types
 * - Token acquisition with silent refresh
 * - Account management and validation
 * 
 * Reference: https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-avoid-page-reloads
 */
export class MSALWrapper {
    private static instance: MSALWrapper;
    private msalInstance: PublicClientApplication;
    private isInitialized: boolean = false;

    private constructor() {
        // Validate configuration before initialization
        validateMsalConfig();
        this.msalInstance = new PublicClientApplication(msalConfig);
    }

    /**
     * Singleton pattern implementation
     * Ensures single MSAL instance across the application
     */
    public static getInstance(): MSALWrapper {
        if (!MSALWrapper.instance) {
            MSALWrapper.instance = new MSALWrapper();
        }
        return MSALWrapper.instance;
    }

    /**
     * Initialize MSAL instance
     * Must be called before any authentication operations
     */
    public async initialize(): Promise<void> {
        if (this.isInitialized) {
            return;
        }

        try {
            await this.msalInstance.initialize();
            this.isInitialized = true;
            console.log('[MSAL Wrapper] Successfully initialized');
        } catch (error) {
            console.error('[MSAL Wrapper] Initialization failed:', error);
            throw new AuthError(
                AuthErrorType.INITIALIZATION_ERROR,
                'Failed to initialize MSAL',
                error
            );
        }
    }

    /**
     * Handle redirect promise after page load
     * Required for redirect-based authentication flows
     */
    public async handleRedirectPromise(): Promise<AuthenticationResult | null> {
        this.ensureInitialized();
        
        try {
            const response = await this.msalInstance.handleRedirectPromise();
            if (response) {
                console.log('[MSAL Wrapper] Handled redirect response successfully');
            }
            return response;
        } catch (error) {
            console.error('[MSAL Wrapper] Error handling redirect promise:', error);
            throw new AuthError(
                AuthErrorType.LOGIN_ERROR,
                'Failed to handle redirect response',
                error
            );
        }
    }

    /**
     * Get all cached accounts
     * Returns array of authenticated accounts
     */
    public getAllAccounts(): AccountInfo[] {
        this.ensureInitialized();
        return this.msalInstance.getAllAccounts();
    }

    /**
     * Login using popup flow
     * Preferred method for better user experience
     */
    public async loginPopup(): Promise<AuthenticationResult> {
        this.ensureInitialized();

        try {
            const response = await this.msalInstance.loginPopup(loginRequest);
            console.log('[MSAL Wrapper] Login successful');
            return response;
        } catch (error) {
            console.error('[MSAL Wrapper] Login failed:', error);
            throw new AuthError(
                AuthErrorType.LOGIN_ERROR,
                'Login failed',
                error
            );
        }
    }

    /**
     * Logout using popup flow
     * Clears tokens and session data
     */
    public async logoutPopup(): Promise<void> {
        this.ensureInitialized();

        const accounts = this.getAllAccounts();
        if (accounts.length === 0) {
            console.warn('[MSAL Wrapper] No accounts to logout');
            return;
        }

        try {
            await this.msalInstance.logoutPopup({
                account: accounts[0],
                postLogoutRedirectUri: window.location.origin,
            });
            console.log('[MSAL Wrapper] Logout successful');
        } catch (error) {
            console.error('[MSAL Wrapper] Logout failed:', error);
            throw new AuthError(
                AuthErrorType.LOGOUT_ERROR,
                'Logout failed',
                error
            );
        }
    }

    /**
     * Acquire access token silently
     * Automatically refreshes expired tokens
     */
    public async acquireTokenSilent(scopes: string[] = loginRequest.scopes): Promise<string | null> {
        this.ensureInitialized();

        const accounts = this.getAllAccounts();
        if (accounts.length === 0) {
            console.warn('[MSAL Wrapper] No accounts available for silent token acquisition');
            return null;
        }

        const silentRequest: SilentRequest = {
            scopes: scopes,
            account: accounts[0],
        };

        try {
            const response = await this.msalInstance.acquireTokenSilent(silentRequest);
            console.log('[MSAL Wrapper] Token acquired silently');
            return response.accessToken;
        } catch (error) {
            if (error instanceof InteractionRequiredAuthError) {
                console.log('[MSAL Wrapper] Silent token acquisition failed, interaction required');
                return null;
            }
            
            console.error('[MSAL Wrapper] Silent token acquisition failed:', error);
            throw new AuthError(
                AuthErrorType.TOKEN_ERROR,
                'Failed to acquire token silently',
                error
            );
        }
    }

    /**
     * Extract user information from account
     * Parses Azure AD token claims for user profile
     */
    public extractUserFromAccount(account: AccountInfo): User {
        return {
            id: account.homeAccountId,
            displayName: account.name || account.username,
            email: account.username,
            department: (account.idTokenClaims as any)?.department,
            jobTitle: (account.idTokenClaims as any)?.jobTitle,
        };
    }

    /**
     * Check if user is authenticated
     * Returns true if valid accounts exist in cache
     */
    public isAuthenticated(): boolean {
        this.ensureInitialized();
        return this.getAllAccounts().length > 0;
    }

    /**
     * Ensure MSAL is initialized before operations
     * Throws error if not initialized
     */
    private ensureInitialized(): void {
        if (!this.isInitialized) {
            throw new AuthError(
                AuthErrorType.INITIALIZATION_ERROR,
                'MSAL instance not initialized. Call initialize() first.'
            );
        }
    }
}

// Export singleton instance
export const msalWrapper = MSALWrapper.getInstance();