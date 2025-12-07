// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';

interface AIHelpButtonProps {
  onAIHelpButtonClick: () => void;
}

const AIHelpButton: React.FC<AIHelpButtonProps> = ({ onAIHelpButtonClick }) => {
  return (
    <div className="fixed bottom-5 right-5 z-[999]">
      <button
        className="bg-gradient-to-r from-blue-600 to-cyan-600 border-0 rounded-full shadow-xl 
                   px-5 py-4 cursor-pointer transition-all duration-200 
                   hover:scale-105 hover:shadow-2xl
                   flex items-center gap-2"
        onClick={onAIHelpButtonClick}
      >
        <span className="text-xl">🤖</span>
        <span className="font-medium text-base text-white">
          Need Help?
        </span>
      </button>
    </div>
  );
};

export default AIHelpButton;