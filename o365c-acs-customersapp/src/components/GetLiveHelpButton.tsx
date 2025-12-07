import React from 'react';

interface GetLiveHelpButtonProps {
  onGetLiveHelpButtonClick: () => void;
}

const GetLiveHelpButton: React.FC<GetLiveHelpButtonProps> = ({ onGetLiveHelpButtonClick }) => {
  return (
    <div className="fixed bottom-5 right-5 z-[999]">
      <button
        onClick={onGetLiveHelpButtonClick}
        className="bg-gradient-to-br from-primary-600 to-secondary-600 border-none rounded-full shadow-floating cursor-pointer px-5 py-4 transition-all duration-200 hover:scale-105 hover:shadow-2xl flex items-center gap-2"
      >
        <span className="text-base">💬</span>
        <span className="font-medium text-base text-white">
          Need Help?
        </span>
      </button>
    </div>
  );
};

export default GetLiveHelpButton;