// Central theme configuration for the application
export const theme = {
  colors: {
    primary: {
      50: '#f5f3ff',
      100: '#ede9fe',
      200: '#ddd6fe',
      300: '#c4b5fd',
      400: '#a78bfa',
      500: '#8b5cf6',
      600: '#7c3aed',
      700: '#6d28d9',
      800: '#5b21b6',
      900: '#4c1d95',
    },
    secondary: {
      50: '#faf5ff',
      100: '#f3e8ff',
      200: '#e9d5ff',
      300: '#d8b4fe',
      400: '#c084fc',
      500: '#a855f7',
      600: '#9333ea',
      700: '#7e22ce',
      800: '#6b21a8',
      900: '#581c87',
    },
    success: '#10b981',
    error: '#ef4444',
    warning: '#f59e0b',
    info: '#3b82f6',
    gray: {
      50: '#f9fafb',
      100: '#f3f4f6',
      200: '#e5e7eb',
      300: '#d1d5db',
      400: '#9ca3af',
      500: '#6b7280',
      600: '#4b5563',
      700: '#374151',
      800: '#1f2937',
      900: '#111827',
    }
  },
  
  gradients: {
    primary: 'linear-gradient(135deg, #8b5cf6 0%, #a855f7 100%)',
    hero: 'linear-gradient(135deg, #dbeafe 0%, #e9d5ff 50%, #e0e7ff 100%)',
    card: 'linear-gradient(to bottom right, #ffffff, #fafafa)',
  },

  shadows: {
    sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
    md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
    lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
    xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
    '2xl': '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
    floating: '0 12px 48px rgba(0, 0, 0, 0.2)',
  },

  borderRadius: {
    sm: '0.25rem',
    md: '0.375rem',
    lg: '0.5rem',
    xl: '0.75rem',
    '2xl': '1rem',
    full: '9999px',
  },

  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem',
    '2xl': '3rem',
    '3xl': '4rem',
  },

  fontSize: {
    xs: '0.75rem',
    sm: '0.875rem',
    base: '1rem',
    lg: '1.125rem',
    xl: '1.25rem',
    '2xl': '1.5rem',
    '3xl': '1.875rem',
    '4xl': '2.25rem',
    '5xl': '3rem',
  },

  fontWeight: {
    normal: '400',
    medium: '500',
    semibold: '600',
    bold: '700',
  },
};

// Reusable component styles
export const componentStyles = {
  button: {
    base: 'px-4 py-2 rounded-lg font-medium transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2',
    primary: 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500 shadow-md hover:shadow-lg',
    secondary: 'bg-gray-200 text-gray-800 hover:bg-gray-300 focus:ring-gray-400',
    ghost: 'bg-transparent text-gray-700 hover:bg-gray-100 focus:ring-gray-300',
    gradient: 'bg-gradient-to-r from-primary-600 to-secondary-600 text-white hover:from-primary-700 hover:to-secondary-700 shadow-lg hover:shadow-xl',
    sizes: {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-4 py-2 text-base',
      lg: 'px-6 py-3 text-lg',
    }
  },

  card: {
    base: 'bg-white rounded-xl shadow-md overflow-hidden',
    hover: 'transition-all duration-200 hover:shadow-lg hover:-translate-y-1',
    bordered: 'border border-gray-200',
    elevated: 'shadow-xl',
  },

  input: {
    base: 'w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
    error: 'border-red-500 focus:ring-red-500',
  },

  avatar: {
    base: 'rounded-full flex items-center justify-center font-semibold',
    sizes: {
      sm: 'w-8 h-8 text-sm',
      md: 'w-10 h-10 text-base',
      lg: 'w-12 h-12 text-lg',
      xl: 'w-16 h-16 text-xl',
    },
    colors: 'bg-primary-600 text-white',
  },

  badge: {
    base: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
    success: 'bg-green-100 text-green-800',
    error: 'bg-red-100 text-red-800',
    warning: 'bg-yellow-100 text-yellow-800',
    info: 'bg-blue-100 text-blue-800',
    primary: 'bg-primary-100 text-primary-800',
  },

  header: {
    container: 'bg-white border-b border-gray-200 py-4',
    content: 'max-w-7xl mx-auto px-4 sm:px-6 lg:px-8',
    flex: 'flex justify-between items-center',
  },

  container: {
    sm: 'max-w-2xl mx-auto px-4',
    md: 'max-w-4xl mx-auto px-4',
    lg: 'max-w-6xl mx-auto px-4',
    xl: 'max-w-7xl mx-auto px-4',
  },

  text: {
    heading1: 'text-4xl font-bold text-gray-900',
    heading2: 'text-3xl font-bold text-gray-900',
    heading3: 'text-2xl font-bold text-gray-900',
    heading4: 'text-xl font-bold text-gray-900',
    body: 'text-base text-gray-700',
    small: 'text-sm text-gray-600',
    muted: 'text-gray-500',
  },

  quickActionCard: {
    container: 'bg-white rounded-lg border border-gray-200 p-3 cursor-pointer transition-all duration-200 hover:shadow-lg hover:-translate-y-1 min-w-[140px]',
    iconWrapper: 'text-2xl w-12 h-12 rounded-lg flex items-center justify-center',
    label: 'text-xs font-medium text-center text-gray-700',
  },

  floatingChat: {
    container: 'fixed bottom-5 right-5 w-[400px] h-[600px] z-[1000] rounded-xl overflow-hidden shadow-2xl',
  },
};

// Quick action items configuration
export const quickActions = [
  { icon: '🔐', label: 'Password Reset', color: '#ef4444' },
  { icon: '📧', label: 'Outlook Issues', color: '#3b82f6' },
  { icon: '📁', label: 'SharePoint Access', color: '#10b981' },
  { icon: '💻', label: 'Teams Problems', color: '#a855f7' },
  { icon: '☁️', label: 'OneDrive Sync', color: '#f59e0b' },
  { icon: '🛡️', label: 'Security Issues', color: '#8b5cf6' },
];

// Helper function to combine class names
export const cn = (...classes: (string | undefined | null | false)[]) => {
  return classes.filter(Boolean).join(' ');
};
