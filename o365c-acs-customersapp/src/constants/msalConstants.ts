export const ERROR_MESSAGES = {
    NO_USERS_SIGNED_IN: "No users are currently signed in",
    MULTIPLE_USERS_SIGNED_IN: "Multiple users are signed in",
    TOKEN_ACQUISITION_FAILED: "Failed to acquire access token",
    INITIALIZATION_FAILED: "MSAL initialization failed",
    LOGIN_FAILED: "Login failed",
    LOGOUT_FAILED: "Logout failed",
} as const;

export const MSAL_SCOPES = {
    GRAPH_USER_READ: "https://graph.microsoft.com/User.Read",
    GRAPH_CALENDARS_READ: "https://graph.microsoft.com/Calendars.Read",
    GRAPH_MAIL_READ: "https://graph.microsoft.com/Mail.Read",
    GRAPH_FILES_READ: "https://graph.microsoft.com/Files.Read",
} as const;

export const DEFAULT_SCOPES = [MSAL_SCOPES.GRAPH_USER_READ];