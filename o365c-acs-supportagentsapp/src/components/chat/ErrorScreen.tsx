// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { threadStrings } from '../../constants/constants';

export interface ErrorScreenProps {
  errorMessage: string;
  isDark?: boolean;
}

export const ErrorScreen = (props: ErrorScreenProps): JSX.Element => {
  const { errorMessage, isDark = true } = props;

  const handleRetry = () => {
    window.location.reload();
  };

  return (
    <div className={`h-full ${isDark ? 'bg-gray-900' : 'bg-gray-50'} flex items-center justify-center p-8`}>
      <div className="text-center max-w-md">
        {/* Error Icon */}
        <div className="mb-6 flex justify-center">
          <div className="w-16 h-16 bg-red-600 rounded-full flex items-center justify-center">
            <svg className="w-8 h-8 text-white" fill="currentColor" viewBox="0 0 20 20">
              <path 
                fillRule="evenodd" 
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" 
                clipRule="evenodd" 
              />
            </svg>
          </div>
        </div>

        {/* Error Title */}
        <h1 className={`text-2xl font-bold ${isDark ? 'text-white' : 'text-gray-900'} mb-4`}>
          {threadStrings.errorScreenTitle}
        </h1>

        {/* Error Message */}
        <p className={`${isDark ? 'text-gray-300' : 'text-gray-600'} mb-8 leading-relaxed`}>
          {errorMessage}
        </p>

        {/* Retry Button */}
        <button
          onClick={handleRetry}
          className="
            bg-blue-700 hover:bg-blue-600 text-white px-6 py-3 rounded-lg
            font-medium transition-all duration-200 shadow-md
            hover:shadow-lg hover:scale-105 flex items-center justify-center mx-auto space-x-2
          "
        >
          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path 
              fillRule="evenodd" 
              d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" 
              clipRule="evenodd" 
            />
          </svg>
          <span>Try Again</span>
        </button>
      </div>
    </div>
  );
};
