import React from 'react';
import { cn } from '../theme';

export interface InputSectionProps {
    labelText: string;
    placeholder: string;
    isEmpty: boolean;
    emptyErrorMessage: string;
    onTextChangedHandler: (value: string) => void;
    onKeyDownHandler: () => void;
    isMultiline?: boolean;
    value?: string; // Add value prop for controlled input
    readonly?: boolean; // Add readonly prop for auto-populated fields
}

export const InputSection: React.FC<InputSectionProps> = ({
    labelText,
    placeholder,
    isEmpty,
    emptyErrorMessage,
    onTextChangedHandler,
    onKeyDownHandler,
    isMultiline = false,
    value = '', // Default to empty string if no value provided
    readonly = false // Default to editable
}) => {
    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !isMultiline) {
            onKeyDownHandler();
        }
    };

    const inputClassName = cn(
        "w-full px-3 py-2 border rounded text-sm outline-none font-inherit",
        isEmpty ? "border-red-500" : "border-gray-300",
        readonly ? "bg-gray-50 cursor-default" : "bg-transparent cursor-text",
        "focus:ring-2 focus:ring-primary-500"
    );

    return (
        <div>
            <label className="block text-sm font-medium mb-2">
                {labelText}
            </label>
            {isMultiline ? (
                <textarea
                    value={value}
                    placeholder={placeholder}
                    onChange={(e) => readonly ? undefined : onTextChangedHandler(e.target.value)}
                    onKeyDown={handleKeyDown}
                    readOnly={readonly}
                    className={cn(inputClassName, "min-h-[80px] resize-y")}
                />
            ) : (
                <input
                    type="text"
                    value={value}
                    placeholder={placeholder}
                    onChange={(e) => readonly ? undefined : onTextChangedHandler(e.target.value)}
                    onKeyDown={handleKeyDown}
                    readOnly={readonly}
                    className={inputClassName}
                />
            )}
            {isEmpty && (
                <span className="block text-xs text-red-500 mt-1">
                    {emptyErrorMessage}
                </span>
            )}
        </div>
    );
};
