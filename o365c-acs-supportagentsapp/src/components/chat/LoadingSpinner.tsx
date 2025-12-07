// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

interface LoadingSpinnerProps {
  label?: string;
}

export const LoadingSpinner = (props: LoadingSpinnerProps): JSX.Element => {
  const { label } = props;
  
  return (
    <div className="flex flex-col items-center justify-center p-8 space-y-4">
      {/* Custom animated spinner */}
      <div className="relative">
        <div className="w-12 h-12 border-4 border-gray-600 border-t-blue-500 rounded-full animate-spin"></div>
        <div className="absolute inset-0 w-12 h-12 border-4 border-transparent border-r-teal-400 rounded-full animate-spin" style={{ animationDirection: 'reverse', animationDuration: '1.5s' }}></div>
      </div>
      
      {/* Loading dots */}
      <div className="flex space-x-2">
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"></div>
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
      </div>
      
      {label && (
        <p className="text-gray-400 text-sm font-medium">{label}</p>
      )}
    </div>
  );
};
