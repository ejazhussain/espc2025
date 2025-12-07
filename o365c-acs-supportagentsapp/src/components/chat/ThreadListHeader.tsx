// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { useTheme } from '../../styles/ThemeProvider';

export interface ThreadListHeaderProps {
  tabs: string[];
  selectedTab: string;
  onStatusTabSelected(tabValue: string): void;
  isDark?: boolean;
  incomingQueueCount?: number;
}

export const ThreadListHeader = (props: ThreadListHeaderProps): JSX.Element => {
  const { tabs, selectedTab, onStatusTabSelected, isDark = true, incomingQueueCount = 0 } = props;
  const { themeClasses } = useTheme();

  return (
    <div>
      {/* Header Title */}
      <div className="mb-6">
        <h2 className={`text-lg font-medium ${isDark ? 'text-white' : 'text-gray-800'}`}>Support Conversations</h2>
      </div>
      
      {/* Tab Navigation */}
      <div className={`flex space-x-1 ${isDark ? 'bg-gray-800' : 'bg-gray-50'} p-1 rounded-lg`}>
        {tabs.map((tab) => {
          const isSelected = tab === selectedTab;
          const showBadge = tab === "Incoming Queue" && incomingQueueCount > 0;

          return (
            <button
              key={tab}
              onClick={() => onStatusTabSelected(tab)}
              className={`
                flex-1 px-3 py-2 text-sm font-normal rounded-md transition-all duration-200 border relative
                ${isSelected
                  ? `${themeClasses.activeTab} text-white shadow-sm border-white/20`
                  : isDark
                    ? 'text-gray-400 hover:text-gray-200 hover:bg-gray-700 border-gray-600 hover:border-gray-500'
                    : 'text-gray-600 hover:text-gray-900 hover:bg-gray-200 border-gray-300 hover:border-gray-400'
                }
              `}
            >
              <span className="flex items-center justify-center gap-2 text-xs">
                {tab}
                {showBadge && (
                  <span className="bg-red-600 text-white text-xs font-bold px-2 py-0.5 rounded-full">
                    {incomingQueueCount}
                  </span>
                )}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
};
