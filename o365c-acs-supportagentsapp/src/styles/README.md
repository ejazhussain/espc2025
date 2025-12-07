# Theme System Documentation

## Overview
This application uses a centralized theme system that allows you to easily switch between different color themes without updating individual components.

## Available Themes

### 🟣 **Violet Theme** (Current) - Modern & Premium
- **Primary Color**: Purple/Violet (`#7c3aed`)
- **Style**: Modern, premium, sophisticated
- **Best for**: SaaS applications, premium services

### 🟢 **Emerald Theme** - Professional & Trustworthy  
- **Primary Color**: Green (`#059669`)
- **Style**: Professional, trustworthy, reliable
- **Best for**: Support applications, financial services

### 🔵 **Blue Theme** - Classic & Professional
- **Primary Color**: Blue (`#2563eb`)
- **Style**: Classic, corporate, traditional
- **Best for**: Enterprise applications, corporate tools

### 🌹 **Rose Theme** - Friendly & Approachable
- **Primary Color**: Pink-Red (`#e11d48`)
- **Style**: Friendly, warm, approachable
- **Best for**: Customer service, social applications

### 🟡 **Amber Theme** - Energetic & Warm
- **Primary Color**: Orange-Yellow (`#d97706`)
- **Style**: Energetic, warm, inviting
- **Best for**: Creative tools, marketing applications

### 🟦 **Indigo Theme** - Professional & Sophisticated
- **Primary Color**: Deep Blue-Purple (`#4f46e5`)
- **Style**: Professional, sophisticated, premium
- **Best for**: Business applications, consulting tools

### 🌊 **Cyan Theme** - Fresh & Modern
- **Primary Color**: Light Blue (`#0891b2`)
- **Style**: Fresh, modern, clean
- **Best for**: Tech startups, development tools

### ⚫ **Slate Theme** - Minimalist & Clean
- **Primary Color**: Gray (`#475569`)
- **Style**: Minimalist, clean, neutral
- **Best for**: Documentation, admin panels

### 🟠 **Orange Theme** - Vibrant & Energetic
- **Primary Color**: Orange (`#ea580c`)
- **Style**: Vibrant, energetic, bold
- **Best for**: Creative agencies, design tools

### 🟢 **Teal Theme** - Balanced & Professional
- **Primary Color**: Teal (`#0d9488`)
- **Style**: Balanced, professional, calming
- **Best for**: Healthcare, wellness applications

### 💜 **Purple Theme** - Creative & Premium
- **Primary Color**: Purple (`#9333ea`)
- **Style**: Creative, premium, artistic
- **Best for**: Design tools, creative platforms

## How to Switch Themes

### Method 1: Simple Theme Switch (Recommended)
1. Open `src/styles/theme.ts`
2. Find the line: `export const currentTheme: Theme = violetTheme;`
3. Replace with your desired theme:
   ```typescript
   export const currentTheme: Theme = violetTheme;    // Purple theme (current)
   export const currentTheme: Theme = emeraldTheme;   // Green theme
   export const currentTheme: Theme = blueTheme;      // Blue theme
   export const currentTheme: Theme = roseTheme;      // Pink-Red theme
   export const currentTheme: Theme = amberTheme;     // Orange-Yellow theme
   export const currentTheme: Theme = indigoTheme;    // Deep blue-purple theme
   export const currentTheme: Theme = cyanTheme;      // Light blue theme
   export const currentTheme: Theme = slateTheme;     // Gray minimalist theme
   export const currentTheme: Theme = orangeTheme;    // Vibrant orange theme
   export const currentTheme: Theme = tealTheme;      // Balanced teal theme
   export const currentTheme: Theme = purpleTheme;    // Creative purple theme
   ```
4. Save the file - the entire application will update automatically!

### Method 2: Create Your Own Custom Theme
1. In `src/styles/theme.ts`, add a new theme object:
   ```typescript
   export const myCustomTheme: Theme = {
     name: 'custom',
     colors: {
       primary: {
         50: '#f0f9ff',    // Very light background
         100: '#e0f2fe',   // Light background  
         500: '#0ea5e9',   // Border/accent color
         600: '#0284c7',   // Main color (avatars, buttons)
         700: '#0369a1',   // Dark color (button hover)
       },
       accent: {
         400: '#06b6d4',   // Status indicator
         500: '#0891b2',   // Active status
         600: '#0e7490',   // Dark accent
       }
     }
   };
   ```
2. Update the current theme:
   ```typescript
   export const currentTheme: Theme = myCustomTheme;
   ```

## Theme Components Affected
The theme system automatically updates:
- ✅ **Avatar backgrounds** in thread list and chat header
- ✅ **Selected thread borders** and backgrounds
- ✅ **Active tab styling** in thread list header
- ✅ **Resolve button** colors and hover states
- ✅ **Status indicators** and text colors

## File Structure
```
src/styles/
├── theme.ts          # Theme definitions and current theme setting
├── ThemeProvider.tsx  # React context provider for theme
└── README.md         # This documentation
```

## Benefits
- 🎨 **One-line theme changes** - Change entire app theme by modifying one line
- 🔧 **No component updates needed** - All components automatically use new theme
- 🎯 **Type-safe** - TypeScript ensures all theme properties are valid
- 🚀 **Performance optimized** - CSS variables for smooth theme switching
- 📱 **Consistent styling** - All components follow the same theme rules

## Quick Start Examples
**Want to try the minimalist gray theme?**
1. Open `src/styles/theme.ts`
2. Change line: `export const currentTheme: Theme = slateTheme;`
3. Save and see your app transform to a clean, minimal design! ⚫

**Want a vibrant orange theme?**
1. Change to: `export const currentTheme: Theme = orangeTheme;`
2. Save and enjoy the energetic orange styling! �

**Want the friendly rose theme?**
1. Change to: `export const currentTheme: Theme = roseTheme;`
2. Save and experience the warm, approachable pink styling! 🌹

**Want the professional teal theme?**
1. Change to: `export const currentTheme: Theme = tealTheme;`
2. Save and enjoy the balanced, calming teal colors! �
