# Auth Module Structure

This auth module provides enterprise-grade Azure AD authentication following Microsoft's best practices.

## 📁 File Structure

```
src/auth/
├── index.ts           # Export barrel - clean imports
├── msalConfig.ts      # MSAL configuration & validation
├── MSALWrapper.ts     # MSAL operations wrapper (singleton)
├── AuthContext.tsx    # React context & provider
└── types.ts           # TypeScript interfaces & errors
```

## 🔧 Configuration

### Environment Variables
Create `.env` file:
```bash
REACT_APP_CLIENT_ID=your-azure-ad-client-id
REACT_APP_AUTHORITY=https://login.microsoftonline.com/common
```

### Azure AD App Registration
1. **Redirect URI**: `http://localhost:3000` (SPA type)
2. **Permissions**: `User.Read` (Microsoft Graph)
3. **Token configuration**: Enable ID tokens

## 🚀 Usage

### Basic Setup
```typescript
// index.tsx
import { AuthProvider } from './auth';

<AuthProvider>
  <App />
</AuthProvider>
```

### Component Usage
```typescript
// Any component
import { useAuth } from './auth';

const MyComponent = () => {
  const { isAuthenticated, user, login, logout, error } = useAuth();
  
  if (error) return <div>Error: {error}</div>;
  
  return (
    <div>
      {isAuthenticated ? (
        <div>Welcome {user?.displayName}</div>
      ) : (
        <button onClick={login}>Sign In</button>
      )}
    </div>
  );
};
```

### Advanced Usage
```typescript
// Direct MSAL wrapper access
import { msalWrapper } from './auth';

// Get access token for API calls
const token = await msalWrapper.acquireTokenSilent(['https://graph.microsoft.com/User.Read']);
```

## 🔒 Security Features

✅ **Environment-based configuration**  
✅ **No hardcoded credentials**  
✅ **PII logging disabled**  
✅ **Secure token storage** (localStorage)  
✅ **Token refresh** with silent acquisition  
✅ **Proper error handling** with custom types  
✅ **Singleton pattern** for consistent MSAL instance  

## 🏗️ Architecture

### Separation of Concerns
- **`msalConfig.ts`**: Pure configuration, no logic
- **`MSALWrapper.ts`**: MSAL operations, error handling
- **`AuthContext.tsx`**: React state management
- **`types.ts`**: TypeScript definitions
- **`index.ts`**: Clean export interface

### Error Handling
Custom `AuthError` class with specific error types:
- `INITIALIZATION_ERROR`
- `LOGIN_ERROR` 
- `LOGOUT_ERROR`
- `TOKEN_ERROR`

### Performance
- **Singleton MSAL instance** prevents memory leaks
- **Silent token refresh** reduces user interruptions
- **Lazy initialization** improves startup time

## 🔄 Migration from AuthService.tsx

The old `AuthService.tsx` has been split into focused modules:

| Old | New | Purpose |
|-----|-----|---------|
| `AuthService.tsx` | `msalConfig.ts` | Configuration |
| `AuthService.tsx` | `MSALWrapper.ts` | MSAL operations |
| `AuthService.tsx` | `AuthContext.tsx` | React context |
| `AuthService.tsx` | `types.ts` | Type definitions |

## 📚 References

- [MSAL.js Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-initializing-client-applications)
- [Azure AD App Registration Guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Microsoft Graph Permissions](https://docs.microsoft.com/en-us/graph/permissions-reference)