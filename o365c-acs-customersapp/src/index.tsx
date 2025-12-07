import React from 'react';
import ReactDOM from 'react-dom/client';
// import './index.css'; // REMOVED - test with NO CSS at all
import App from './App';
import { AuthProvider } from './auth';
import { TestACSCall } from './TestACSCall';

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);

// Toggle this to test ACS in isolation
const SHOW_ACS_TEST = false;

root.render(
  <React.StrictMode>
    {SHOW_ACS_TEST ? (
      <TestACSCall />
    ) : (
      <AuthProvider>
        <App />
      </AuthProvider>
    )}
  </React.StrictMode>
);