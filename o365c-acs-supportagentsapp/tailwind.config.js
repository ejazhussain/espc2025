/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Legacy colors
        'dark-primary': '#0f172a',
        'dark-secondary': '#1e293b',
        'dark-accent': '#334155',
        'blue-primary': '#3b82f6',
        'blue-secondary': '#1d4ed8',
        'teal-accent': '#14b8a6',
        
        // Dynamic theme colors using CSS variables
        'theme': {
          'primary-50': 'rgb(var(--color-primary-50) / <alpha-value>)',
          'primary-100': 'rgb(var(--color-primary-100) / <alpha-value>)',
          'primary-500': 'rgb(var(--color-primary-500) / <alpha-value>)',
          'primary-600': 'rgb(var(--color-primary-600) / <alpha-value>)',
          'primary-700': 'rgb(var(--color-primary-700) / <alpha-value>)',
          'accent-400': 'rgb(var(--color-accent-400) / <alpha-value>)',
          'accent-500': 'rgb(var(--color-accent-500) / <alpha-value>)',
          'accent-600': 'rgb(var(--color-accent-600) / <alpha-value>)',
        }
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
  safelist: [
    // All theme color variations
    'bg-violet-600', 'bg-violet-700', 'hover:bg-violet-600', 'border-violet-500', 'bg-violet-50',
    'bg-emerald-600', 'bg-emerald-700', 'hover:bg-emerald-600', 'border-emerald-500', 'bg-emerald-50',
    'bg-blue-600', 'bg-blue-700', 'hover:bg-blue-600', 'border-blue-500', 'bg-blue-50',
    'bg-rose-600', 'bg-rose-700', 'hover:bg-rose-600', 'border-rose-500', 'bg-rose-50',
    'bg-amber-600', 'bg-amber-700', 'hover:bg-amber-600', 'border-amber-500', 'bg-amber-50',
    'bg-indigo-600', 'bg-indigo-700', 'hover:bg-indigo-600', 'border-indigo-500', 'bg-indigo-50',
    'bg-cyan-600', 'bg-cyan-700', 'hover:bg-cyan-600', 'border-cyan-500', 'bg-cyan-50',
    'bg-slate-600', 'bg-slate-700', 'hover:bg-slate-600', 'border-slate-500', 'bg-slate-50',
    'bg-orange-600', 'bg-orange-700', 'hover:bg-orange-600', 'border-orange-500', 'bg-orange-50',
    'bg-teal-600', 'bg-teal-700', 'hover:bg-teal-600', 'border-teal-500', 'bg-teal-50',
    'bg-purple-600', 'bg-purple-700', 'hover:bg-purple-600', 'border-purple-500', 'bg-purple-50',
    // Microsoft Teams specific colors
    'bg-[#6264a7]', 'bg-[#464775]', 'hover:bg-[#6264a7]', 'border-[#7b83eb]', 'bg-[#f5f5ff]',
    'bg-[#0078d4]', 'text-[#0078d4]', 'bg-[#00bcf2]', 'text-[#005a9e]',
    // Green status indicators for active chats
    'bg-green-500', 'text-green-600', 'bg-green-400', 'text-green-500',
    // Status pill/badge colors
    'bg-green-100', 'text-green-800', 'border-green-200', 'bg-gray-100', 'text-gray-600', 'border-gray-200',
    // Status indicators
    'bg-teal-500', 'text-teal-600', 'bg-cyan-500', 'text-cyan-600', 
    'bg-amber-500', 'text-amber-600', 'bg-blue-500', 'text-blue-600',
  ],
}
