// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { createContext, useContext, useEffect } from 'react';
import { Theme, getCSSVariables, currentTheme } from './theme';

// Utility function to calculate relative luminance of a color
const getLuminance = (hexColor: string): number => {
  // Remove # if present
  const hex = hexColor.replace('#', '');
  
  // Convert hex to RGB
  const r = parseInt(hex.substr(0, 2), 16) / 255;
  const g = parseInt(hex.substr(2, 2), 16) / 255;
  const b = parseInt(hex.substr(4, 2), 16) / 255;
  
  // Apply gamma correction
  const sRGB = [r, g, b].map(c => {
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
  });
  
  // Calculate luminance
  return 0.2126 * sRGB[0] + 0.7152 * sRGB[1] + 0.0722 * sRGB[2];
};

// Function to determine if text should be light or dark based on background
const getTextColorForBackground = (backgroundColor: string): string => {
  const luminance = getLuminance(backgroundColor);
  // Use WCAG contrast ratio guidelines: if background is light, use dark text
  return luminance > 0.5 ? 'text-gray-900' : 'text-white';
};

// Theme color to hex mapping for accessibility calculations
const themeColorHexMap: Record<string, string> = {
  'bg-violet-600': '#7c3aed',
  'bg-emerald-600': '#059669',
  'bg-blue-600': '#2563eb',
  'bg-rose-600': '#e11d48',
  'bg-amber-600': '#d97706',
  'bg-indigo-600': '#4f46e5',
  'bg-cyan-600': '#0891b2',
  'bg-slate-600': '#475569',
  'bg-orange-600': '#ea580c',
  'bg-teal-600': '#0d9488',
  'bg-purple-600': '#9333ea',
  'bg-[#6264a7]': '#6264a7', // Teams color
};

interface ThemeContextType {
  theme: Theme;
  themeClasses: {
    avatar: string;
    avatarText: string; // New property for accessible text color
    button: string;
    activeTab: string;
    selectedBorder: string;
    selectedBgLight: string;
    selectedBgDark: string;
    statusDot: string;
    statusText: string;
  };
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const useTheme = (): ThemeContextType => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
};

interface ThemeProviderProps {
  children: React.ReactNode;
}

export const ThemeProvider: React.FC<ThemeProviderProps> = ({ children }) => {
  const theme = currentTheme;

  // Inject CSS custom properties for Microsoft Teams theme
  useEffect(() => {
    const root = document.documentElement;
    
    // Apply Microsoft Teams theme colors as CSS custom properties
    if (theme.name === 'teams') {
      root.style.setProperty('--teams-primary-50', '#f5f5ff');
      root.style.setProperty('--teams-primary-100', '#ebebff');
      root.style.setProperty('--teams-primary-500', '#7b83eb');
      root.style.setProperty('--teams-primary-600', '#6264a7');
      root.style.setProperty('--teams-primary-700', '#464775');
      root.style.setProperty('--teams-accent-400', '#00bcf2');
      root.style.setProperty('--teams-accent-500', '#0078d4');
      root.style.setProperty('--teams-accent-600', '#005a9e');
    }
  }, [theme.name]);

  // Generate theme classes based on current theme name
  const getThemeClasses = () => {
    switch (theme.name) {
      case 'violet':
        const violetAvatarBg = 'bg-violet-600';
        const violetTextColor = getTextColorForBackground(themeColorHexMap[violetAvatarBg]);
        return {
          avatar: violetAvatarBg,
          avatarText: violetTextColor,
          button: 'bg-violet-700 hover:bg-violet-600',
          activeTab: 'bg-violet-600',
          selectedBorder: 'border-violet-500',
          selectedBgLight: 'bg-violet-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'emerald':
        const emeraldAvatarBg = 'bg-emerald-600';
        const emeraldTextColor = getTextColorForBackground(themeColorHexMap[emeraldAvatarBg]);
        return {
          avatar: emeraldAvatarBg,
          avatarText: emeraldTextColor,
          button: 'bg-emerald-700 hover:bg-emerald-600',
          activeTab: 'bg-emerald-600',
          selectedBorder: 'border-emerald-500',
          selectedBgLight: 'bg-emerald-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'blue':
        const blueAvatarBg = 'bg-blue-600';
        const blueTextColor = getTextColorForBackground(themeColorHexMap[blueAvatarBg]);
        return {
          avatar: blueAvatarBg,
          avatarText: blueTextColor,
          button: 'bg-blue-700 hover:bg-blue-600',
          activeTab: 'bg-blue-600',
          selectedBorder: 'border-blue-500',
          selectedBgLight: 'bg-blue-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'rose':
        const roseAvatarBg = 'bg-rose-600';
        const roseTextColor = getTextColorForBackground(themeColorHexMap[roseAvatarBg]);
        return {
          avatar: roseAvatarBg,
          avatarText: roseTextColor,
          button: 'bg-rose-700 hover:bg-rose-600',
          activeTab: 'bg-rose-600',
          selectedBorder: 'border-rose-500',
          selectedBgLight: 'bg-rose-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'amber':
        const amberAvatarBg = 'bg-amber-600';
        const amberTextColor = getTextColorForBackground(themeColorHexMap[amberAvatarBg]);
        return {
          avatar: amberAvatarBg,
          avatarText: amberTextColor, // This will be dark text for better contrast
          button: 'bg-amber-700 hover:bg-amber-600',
          activeTab: 'bg-amber-600',
          selectedBorder: 'border-amber-500',
          selectedBgLight: 'bg-amber-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'indigo':
        const indigoAvatarBg = 'bg-indigo-600';
        const indigoTextColor = getTextColorForBackground(themeColorHexMap[indigoAvatarBg]);
        return {
          avatar: indigoAvatarBg,
          avatarText: indigoTextColor,
          button: 'bg-indigo-700 hover:bg-indigo-600',
          activeTab: 'bg-indigo-600',
          selectedBorder: 'border-indigo-500',
          selectedBgLight: 'bg-indigo-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
      case 'cyan':
        const cyanAvatarBg = 'bg-cyan-600';
        const cyanTextColor = getTextColorForBackground(themeColorHexMap[cyanAvatarBg]);
        return {
          avatar: cyanAvatarBg,
          avatarText: cyanTextColor,
          button: 'bg-cyan-700 hover:bg-cyan-600',
          activeTab: 'bg-cyan-600',
          selectedBorder: 'border-cyan-500',
          selectedBgLight: 'bg-cyan-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-amber-500',
          statusText: 'text-amber-600',
        };
      case 'slate':
        const slateAvatarBg = 'bg-slate-600';
        const slateTextColor = getTextColorForBackground(themeColorHexMap[slateAvatarBg]);
        return {
          avatar: slateAvatarBg,
          avatarText: slateTextColor,
          button: 'bg-slate-700 hover:bg-slate-600',
          activeTab: 'bg-slate-600',
          selectedBorder: 'border-slate-500',
          selectedBgLight: 'bg-slate-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-cyan-500',
          statusText: 'text-cyan-600',
        };
      case 'orange':
        const orangeAvatarBg = 'bg-orange-600';
        const orangeTextColor = getTextColorForBackground(themeColorHexMap[orangeAvatarBg]);
        return {
          avatar: orangeAvatarBg,
          avatarText: orangeTextColor,
          button: 'bg-orange-700 hover:bg-orange-600',
          activeTab: 'bg-orange-600',
          selectedBorder: 'border-orange-500',
          selectedBgLight: 'bg-orange-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-cyan-500',
          statusText: 'text-cyan-600',
        };
      case 'teal':
        const tealAvatarBg = 'bg-teal-600';
        const tealTextColor = getTextColorForBackground(themeColorHexMap[tealAvatarBg]);
        return {
          avatar: tealAvatarBg,
          avatarText: tealTextColor,
          button: 'bg-teal-700 hover:bg-teal-600',
          activeTab: 'bg-teal-600',
          selectedBorder: 'border-teal-500',
          selectedBgLight: 'bg-teal-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-amber-500',
          statusText: 'text-amber-600',
        };
      case 'purple':
        const purpleAvatarBg = 'bg-purple-600';
        const purpleTextColor = getTextColorForBackground(themeColorHexMap[purpleAvatarBg]);
        return {
          avatar: purpleAvatarBg,
          avatarText: purpleTextColor,
          button: 'bg-purple-700 hover:bg-purple-600',
          activeTab: 'bg-purple-600',
          selectedBorder: 'border-purple-500',
          selectedBgLight: 'bg-purple-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-cyan-500',
          statusText: 'text-cyan-600',
        };
      case 'teams':
        const teamsAvatarBg = 'bg-[#6264a7]';
        const teamsTextColor = getTextColorForBackground(themeColorHexMap[teamsAvatarBg]);
        return {
          avatar: teamsAvatarBg,                    // Official Microsoft Teams purple
          avatarText: teamsTextColor,
          button: 'bg-[#464775] hover:bg-[#6264a7]', // Dark Teams purple with hover
          activeTab: 'bg-[#6264a7]',                 // Teams purple for active tab
          selectedBorder: 'border-[#7b83eb]',        // Teams border accent
          selectedBgLight: 'bg-[#f5f5ff]',          // Very light Teams background
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-green-500',                // Green for active status (more intuitive)
          statusText: 'text-green-600',             // Green text for active status
        };
      default:
        const defaultAvatarBg = 'bg-violet-600';
        const defaultTextColor = getTextColorForBackground(themeColorHexMap[defaultAvatarBg]);
        return {
          avatar: defaultAvatarBg,
          avatarText: defaultTextColor,
          button: 'bg-violet-700 hover:bg-violet-600',
          activeTab: 'bg-violet-600',
          selectedBorder: 'border-violet-500',
          selectedBgLight: 'bg-violet-50',
          selectedBgDark: 'bg-gray-800',
          statusDot: 'bg-teal-500',
          statusText: 'text-teal-600',
        };
    }
  };

  const themeClasses = getThemeClasses();

  // Inject CSS variables into the document root
  useEffect(() => {
    const root = document.documentElement;
    const cssVars = getCSSVariables();
    
    Object.entries(cssVars).forEach(([property, value]) => {
      root.style.setProperty(property, value);
    });

    // Cleanup function
    return () => {
      Object.keys(cssVars).forEach((property) => {
        root.style.removeProperty(property);
      });
    };
  }, [theme]);

  return (
    <ThemeContext.Provider value={{ theme, themeClasses }}>
      {children}
    </ThemeContext.Provider>
  );
};
