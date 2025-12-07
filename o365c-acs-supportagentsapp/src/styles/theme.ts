// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ThemeColors {
  primary: {
    50: string;
    100: string;
    500: string;
    600: string;
    700: string;
  };
  accent: {
    400: string;
    500: string;
    600: string;
  };
}

export interface Theme {
  name: string;
  colors: ThemeColors;
}

// Violet (Purple) - Modern & Premium Theme
export const violetTheme: Theme = {
  name: 'violet',
  colors: {
    primary: {
      50: '#f5f3ff',    // Very light violet background
      100: '#ede9fe',   // Light violet background
      500: '#8b5cf6',   // Violet border/accent
      600: '#7c3aed',   // Main violet (avatars, buttons)
      700: '#6d28d9',   // Dark violet (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Emerald (Green) - Professional & Trustworthy Theme
export const emeraldTheme: Theme = {
  name: 'emerald',
  colors: {
    primary: {
      50: '#ecfdf5',    // Very light emerald background
      100: '#d1fae5',   // Light emerald background
      500: '#10b981',   // Emerald border/accent
      600: '#059669',   // Main emerald (avatars, buttons)
      700: '#047857',   // Dark emerald (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Blue - Classic & Professional Theme
export const blueTheme: Theme = {
  name: 'blue',
  colors: {
    primary: {
      50: '#eff6ff',    // Very light blue background
      100: '#dbeafe',   // Light blue background
      500: '#3b82f6',   // Blue border/accent
      600: '#2563eb',   // Main blue (avatars, buttons)
      700: '#1d4ed8',   // Dark blue (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Rose (Pink-Red) - Friendly & Approachable Theme
export const roseTheme: Theme = {
  name: 'rose',
  colors: {
    primary: {
      50: '#fff1f2',    // Very light rose background
      100: '#ffe4e6',   // Light rose background
      500: '#f43f5e',   // Rose border/accent
      600: '#e11d48',   // Main rose (avatars, buttons)
      700: '#be123c',   // Dark rose (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Amber (Orange-Yellow) - Energetic & Warm Theme
export const amberTheme: Theme = {
  name: 'amber',
  colors: {
    primary: {
      50: '#fffbeb',    // Very light amber background
      100: '#fef3c7',   // Light amber background
      500: '#f59e0b',   // Amber border/accent
      600: '#d97706',   // Main amber (avatars, buttons)
      700: '#b45309',   // Dark amber (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Indigo (Deep Blue-Purple) - Professional & Sophisticated Theme
export const indigoTheme: Theme = {
  name: 'indigo',
  colors: {
    primary: {
      50: '#eef2ff',    // Very light indigo background
      100: '#e0e7ff',   // Light indigo background
      500: '#6366f1',   // Indigo border/accent
      600: '#4f46e5',   // Main indigo (avatars, buttons)
      700: '#4338ca',   // Dark indigo (button hover)
    },
    accent: {
      400: '#06b6d4',   // Teal for status indicators
      500: '#0891b2',   // Teal for active status
      600: '#0e7490',   // Dark teal
    }
  }
};

// Cyan (Light Blue) - Fresh & Modern Theme
export const cyanTheme: Theme = {
  name: 'cyan',
  colors: {
    primary: {
      50: '#ecfeff',    // Very light cyan background
      100: '#cffafe',   // Light cyan background
      500: '#06b6d4',   // Cyan border/accent
      600: '#0891b2',   // Main cyan (avatars, buttons)
      700: '#0e7490',   // Dark cyan (button hover)
    },
    accent: {
      400: '#f59e0b',   // Amber for status indicators
      500: '#d97706',   // Amber for active status
      600: '#b45309',   // Dark amber
    }
  }
};

// Slate (Gray) - Minimalist & Clean Theme
export const slateTheme: Theme = {
  name: 'slate',
  colors: {
    primary: {
      50: '#f8fafc',    // Very light slate background
      100: '#f1f5f9',   // Light slate background
      500: '#64748b',   // Slate border/accent
      600: '#475569',   // Main slate (avatars, buttons)
      700: '#334155',   // Dark slate (button hover)
    },
    accent: {
      400: '#06b6d4',   // Cyan for status indicators
      500: '#0891b2',   // Cyan for active status
      600: '#0e7490',   // Dark cyan
    }
  }
};

// Orange - Vibrant & Energetic Theme
export const orangeTheme: Theme = {
  name: 'orange',
  colors: {
    primary: {
      50: '#fff7ed',    // Very light orange background
      100: '#ffedd5',   // Light orange background
      500: '#f97316',   // Orange border/accent
      600: '#ea580c',   // Main orange (avatars, buttons)
      700: '#c2410c',   // Dark orange (button hover)
    },
    accent: {
      400: '#06b6d4',   // Cyan for status indicators
      500: '#0891b2',   // Cyan for active status
      600: '#0e7490',   // Dark cyan
    }
  }
};

// Teal - Balanced & Professional Theme
export const tealTheme: Theme = {
  name: 'teal',
  colors: {
    primary: {
      50: '#f0fdfa',    // Very light teal background
      100: '#ccfbf1',   // Light teal background
      500: '#14b8a6',   // Teal border/accent
      600: '#0d9488',   // Main teal (avatars, buttons)
      700: '#0f766e',   // Dark teal (button hover)
    },
    accent: {
      400: '#f59e0b',   // Amber for status indicators
      500: '#d97706',   // Amber for active status
      600: '#b45309',   // Dark amber
    }
  }
};

// Purple - Creative & Premium Theme
export const purpleTheme: Theme = {
  name: 'purple',
  colors: {
    primary: {
      50: '#faf5ff',    // Very light purple background
      100: '#f3e8ff',   // Light purple background
      500: '#a855f7',   // Purple border/accent
      600: '#9333ea',   // Main purple (avatars, buttons)
      700: '#7c3aed',   // Dark purple (button hover)
    },
    accent: {
      400: '#06b6d4',   // Cyan for status indicators
      500: '#0891b2',   // Cyan for active status
      600: '#0e7490',   // Dark cyan
    }
  }
};

// Microsoft Teams - Official Teams Purple Theme
export const teamsTheme: Theme = {
  name: 'teams',
  colors: {
    primary: {
      50: '#f5f5ff',    // Very light Teams background
      100: '#ebebff',   // Light Teams background
      500: '#7b83eb',   // Teams border/accent
      600: '#6264a7',   // Official Microsoft Teams purple
      700: '#464775',   // Dark Teams purple (button hover)
    },
    accent: {
      400: '#00bcf2',   // Teams blue for status indicators
      500: '#0078d4',   // Microsoft blue for active status
      600: '#005a9e',   // Dark Microsoft blue
    }
  }
};

// Current active theme - Change this to switch themes globally
export const currentTheme: Theme = teamsTheme;

// Easy theme switching function - Just change the theme name here!
// Available options: violetTheme, emeraldTheme, blueTheme, roseTheme, amberTheme, 
//                   indigoTheme, cyanTheme, slateTheme, orangeTheme, tealTheme, purpleTheme, teamsTheme
export const switchTheme = (newTheme: Theme): Theme => {
  return newTheme;
};

// Theme switching examples:
// export const currentTheme: Theme = violetTheme;    // Purple theme 
// export const currentTheme: Theme = emeraldTheme;   // Green theme
// export const currentTheme: Theme = blueTheme;      // Blue theme
// export const currentTheme: Theme = roseTheme;      // Pink-Red theme
// export const currentTheme: Theme = amberTheme;     // Orange-Yellow theme
// export const currentTheme: Theme = indigoTheme;    // Deep blue-purple theme
// export const currentTheme: Theme = cyanTheme;      // Light blue theme
// export const currentTheme: Theme = slateTheme;     // Gray minimalist theme
// export const currentTheme: Theme = orangeTheme;    // Vibrant orange theme
// export const currentTheme: Theme = tealTheme;      // Balanced teal theme
// export const currentTheme: Theme = purpleTheme;    // Creative purple theme (current)
// export const currentTheme: Theme = teamsTheme;     // Microsoft Teams purple theme

// Theme utility functions
export const getThemeClasses = () => {
  const theme = currentTheme;
  
  return {
    // Avatar styles
    avatar: `bg-[${theme.colors.primary[600]}]`,
    
    // Button styles
    button: `bg-[${theme.colors.primary[700]}] hover:bg-[${theme.colors.primary[600]}]`,
    
    // Tab styles
    activeTab: `bg-[${theme.colors.primary[600]}]`,
    
    // Border styles
    selectedBorder: `border-[${theme.colors.primary[500]}]`,
    
    // Background styles
    selectedBgLight: `bg-[${theme.colors.primary[50]}]`,
    selectedBgDark: 'bg-gray-800',
    
    // Status indicator styles
    statusDot: `bg-[${theme.colors.accent[500]}]`,
    statusText: `text-[${theme.colors.accent[600]}]`,
  };
};

// CSS custom properties for dynamic theming
export const getCSSVariables = () => {
  const theme = currentTheme;
  
  return {
    '--color-primary-50': theme.colors.primary[50],
    '--color-primary-100': theme.colors.primary[100],
    '--color-primary-500': theme.colors.primary[500],
    '--color-primary-600': theme.colors.primary[600],
    '--color-primary-700': theme.colors.primary[700],
    '--color-accent-400': theme.colors.accent[400],
    '--color-accent-500': theme.colors.accent[500],
    '--color-accent-600': theme.colors.accent[600],
  };
};
