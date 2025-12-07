import React, { useState } from 'react';
import './tailwind.css'; // Import Tailwind CSS ONLY for the main app
import './App.css';
import ChatFloatingWindow from './components/ChatFloatingWindow';
import AIHelpButton from './components/AIHelpButton';
import { AIAssistantChat } from './components/AIAssistantChat';
import { useAuth } from './auth';
import { componentStyles, quickActions, cn } from './theme';

interface EscalationData {
  conversationId: string;
  customerName: string;
  customerEmail: string;
  messages: any[];
  problemDescription: string;
}

const App = () => {
  const [isChatFloatingWindowOpen, setIsChatFloatingWindowOpen] = useState(false);
  const [isAIChatOpen, setIsAIChatOpen] = useState(false);
  const [escalationData, setEscalationData] = useState<EscalationData | null>(null);
  
  // Use MSAL authentication context
  const { isAuthenticated, isLoading, user, login, logout, error, clearError } = useAuth();

  // Handle AI escalation to human agent
  const handleAIEscalation = (conversationId: string, messages: any[], escalationData?: EscalationData) => {
    console.log('AI escalating to human agent:', { conversationId, messageCount: messages.length, escalationData });
    
    // Store escalation data to skip the configuration form
    if (escalationData) {
      setEscalationData(escalationData);
    }
    
    // Close AI chat and open human chat flow
    setIsAIChatOpen(false);
    setIsChatFloatingWindowOpen(true);
  };

  // Show loading state during authentication
  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="text-gray-600">Loading...</div>
      </div>
    );
  }

  // Show error state if authentication failed
  if (error) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="bg-white p-5 max-w-md rounded-lg shadow-lg border border-gray-200">
          <div className="text-red-600 font-bold">Authentication Error</div>
          <div className="text-sm mt-2 block">{error}</div>
          <button 
            className="mt-4 px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700 transition-colors"
            onClick={() => { clearError(); login(); }}
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="relative min-h-screen bg-gray-50">
      {/* Header */}
      <header className={componentStyles.header.container}>
        <div className={componentStyles.container.xl}>
          <div className={componentStyles.header.flex}>
            {/* Logo Section */}
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-primary-600 to-secondary-600 rounded-lg flex items-center justify-center text-white text-xl">
                🛠️
              </div>
              <div>
                <div className="text-lg font-bold text-gray-900">TechCorp IT Portal</div>
                <div className="text-xs text-gray-500">Student Support Center</div>
              </div>
            </div>
            
            {/* User Section */}
            {isAuthenticated ? (
              <div className="flex items-center gap-3">
                <div className={cn(componentStyles.avatar.base, componentStyles.avatar.sizes.md, componentStyles.avatar.colors)}>
                  {user?.displayName?.charAt(0) || 'U'}
                </div>
                <div>
                  <div className="text-sm font-medium text-gray-900">{user?.displayName}</div>
                  <div className="text-xs text-gray-500">{user?.department || 'Student'}</div>
                </div>
                <div className="relative group">
                  <button className={cn(componentStyles.button.base, componentStyles.button.ghost, 'px-2')}>
                    ⋯
                  </button>
                  <div className="hidden group-hover:block absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-10">
                    <button 
                      onClick={logout}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                    >
                      Sign Out
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <button 
                onClick={login}
                className={cn(componentStyles.button.base, componentStyles.button.primary)}
              >
                <span className="flex items-center gap-2">
                  <span className="text-base">🔐</span>
                  Sign In with Microsoft
                </span>
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className={cn(componentStyles.container.lg, 'pt-16 pb-16')}>
        {isAuthenticated ? (
          <div className="bg-gradient-to-br from-blue-50 via-purple-50 to-indigo-50 rounded-2xl shadow-xl border-0 overflow-hidden">
            <div className="py-12 px-6">
              <div className="flex flex-col items-center gap-6">
                {/* Welcome Section */}
                <div className="text-center">
                  <h1 className="text-5xl font-bold text-gray-900 mb-3">
                    Welcome back, {user?.displayName?.split(' ')[0]}! 👋
                  </h1>
                  <p className="text-xl text-gray-600 mb-2">
                    Get instant AI-driven support for student IT issues, Microsoft 365, and university apps
                  </p>
                  <p className="text-base text-gray-500 mb-6">
                    Start with AI assistance, escalate to human experts when needed
                  </p>
                </div>
                
                {/* Quick Access Cards */}
                <div className="w-full max-w-2xl">
                  <h3 className="text-base font-medium text-gray-600 mb-4 text-center">
                    Quick Actions
                  </h3>
                  <div className="flex flex-wrap justify-center gap-3">
                    {quickActions.map((item, index) => (
                      <div 
                        key={index}
                        className={componentStyles.quickActionCard.container}
                      >
                        <div className="flex flex-col items-center gap-2">
                          <div 
                            className={componentStyles.quickActionCard.iconWrapper}
                            style={{ 
                              backgroundColor: `${item.color}15`,
                            }}
                          >
                            {item.icon}
                          </div>
                          <div className={componentStyles.quickActionCard.label}>
                            {item.label}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
                
                {/* CTA Button - Start AI Support */}
                <button 
                  className={cn(componentStyles.button.base, componentStyles.button.gradient, 'px-8 py-4 text-lg')}
                  onClick={() => setIsAIChatOpen(true)}
                >
                  <span className="flex items-center gap-3">
                    <span className="text-2xl">🤖</span>
                    <span className="font-bold">Get IT Support</span>
                  </span>
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="bg-gradient-to-br from-blue-50 via-purple-50 to-indigo-50 rounded-2xl shadow-xl border-0 overflow-hidden">
            <div className="py-12 px-6">
              <div className="flex flex-col items-center gap-6">
                <div className="text-center">
                  <h1 className="text-5xl font-bold text-gray-900 mb-3">
                    🛠️ TechCorp IT Helpdesk Portal
                  </h1>
                  <p className="text-xl text-gray-600 mb-2">
                    Enterprise IT Support for Student
                  </p>
                  <p className="text-base text-gray-500">
                    Sign in with your Microsoft account to access personalized IT support
                  </p>
                </div>
                
                <button 
                  className={cn(componentStyles.button.base, componentStyles.button.gradient, 'px-8 py-4 text-lg')}
                  onClick={login}
                >
                  <span className="flex items-center gap-3">
                    <span className="text-2xl">🔐</span>
                    <span className="font-bold">Sign In with Microsoft</span>
                  </span>
                </button>
              </div>
            </div>
          </div>
        )}
      </main>

      {/* Chat Components */}
      {isAIChatOpen ? (
        <div className={componentStyles.floatingChat.container}>
          <AIAssistantChat
            customerName={user?.displayName || 'Customer'}
            customerEmail={user?.email || ''}
            onEscalateToAgent={handleAIEscalation}
            onClose={() => setIsAIChatOpen(false)}
          />
        </div>
      ) : isChatFloatingWindowOpen ? (
        <ChatFloatingWindow
          onCloseButtonClick={() => {
            setIsChatFloatingWindowOpen(false);
            setEscalationData(null); // Clear escalation data when closing
          }}
          userDisplayName={user?.displayName} // Pass user's display name for pre-populating the form
          userEmail={user?.email} // Pass user's email for automatic population
          escalationData={escalationData || undefined} // Pass escalation data to skip the form
        />
      ) : isAuthenticated ? (
        <AIHelpButton
          onAIHelpButtonClick={() => {
            setIsAIChatOpen(true);
          }}
        />
      ) : null}
    </div>
  );
};

export default App;