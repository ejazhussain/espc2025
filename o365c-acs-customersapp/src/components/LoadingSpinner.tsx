// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';

export interface LoadingSpinnerProps {
  label: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ label }) => {
  return (
    <div className="flex flex-col justify-center items-center h-full gap-3">
      <div className="w-6 h-6 border-2 border-gray-300 border-t-primary-600 rounded-full animate-spin" />
      <span className="text-sm text-gray-600">{label}</span>
    </div>
  );
};