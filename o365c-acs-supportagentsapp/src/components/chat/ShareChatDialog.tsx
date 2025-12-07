// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useState, useCallback } from 'react';
import { useTheme } from '../../styles/ThemeProvider';
import { generateChatTranscript } from '../../services/transcriptService';
import { sendTranscriptEmail } from '../../services/emailService';
import { ChatTranscript } from '../../types/chatTranscript';

export interface ShareChatDialogProps {
  isOpen: boolean;
  threadId: string;
  customerName: string;
  onClose: () => void;
  isDark?: boolean;
}

export const ShareChatDialog: React.FC<ShareChatDialogProps> = ({
  isOpen,
  threadId,
  customerName,
  onClose,
  isDark = true
}) => {
  const { themeClasses } = useTheme();
  const [transcript, setTranscript] = useState<ChatTranscript | null>(null);
  const [isGeneratingTranscript, setIsGeneratingTranscript] = useState(false);
  const [isSendingEmail, setIsSendingEmail] = useState(false);
  const [transcriptGenerated, setTranscriptGenerated] = useState(false);
  const [emailSent, setEmailSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [customerEmail, setCustomerEmail] = useState('');
  const [emailValidationError, setEmailValidationError] = useState<string | null>(null);

  // Email validation function
  const validateEmail = useCallback((email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email.trim());
  }, []);

  // Check if email is valid for enabling send button
  const isEmailValid = customerEmail.trim() && validateEmail(customerEmail);

  const handleEmailChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setCustomerEmail(value);
    
    // Clear previous validation error
    setEmailValidationError(null);
    
    // Validate email if user has entered something
    if (value.trim()) {
      if (!validateEmail(value)) {
        setEmailValidationError('Please enter a valid email address');
      }
    }
  }, [validateEmail]);

  const handleGenerateTranscript = useCallback(async () => {
    if (transcriptGenerated) return;
    
    setIsGeneratingTranscript(true);
    setError(null);
    
    try {
      const generatedTranscript = await generateChatTranscript({
        threadId,
        customerName,
        agentName: 'Support Agent'
      });

      setTranscript(generatedTranscript);
      setTranscriptGenerated(true);
      setEmailSent(false);
    } catch (error: unknown) {
      const message =
        (typeof error === 'object' && error && 'response' in error && (error as any).response?.data?.error) ??
        (typeof error === 'object' && error && 'message' in error ? (error as any).message : undefined) ??
        'Failed to generate transcript';
      setError(typeof message === 'string' ? message : 'Failed to generate transcript');
    } finally {
      setIsGeneratingTranscript(false);
    }
  }, [threadId, customerName, transcriptGenerated]);

  const handleSendEmail = useCallback(async () => {
    if (!transcript || emailSent || !customerEmail.trim()) return;
    
    setIsSendingEmail(true);
    setError(null);
    
    try {
      const response = await sendTranscriptEmail(transcript, customerEmail.trim());
      if (response.success) {
        setEmailSent(true);
      } else {
        throw new Error(response.message);
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Failed to send email');
    } finally {
      setIsSendingEmail(false);
    }
  }, [transcript, emailSent, customerEmail]);

  const handleClose = useCallback(() => {
    onClose();
    // Reset state for next use
    setTranscript(null);
    setTranscriptGenerated(false);
    setEmailSent(false);
    setError(null);
    setCustomerEmail('');
    setEmailValidationError(null);
  }, [onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className={`${isDark ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200'} border rounded-lg shadow-xl max-w-4xl w-full mx-4 max-h-[90vh] overflow-y-auto`}>
        {/* Header */}
        <div className={`${isDark ? 'border-gray-700' : 'border-gray-200'} border-b p-6`}>
          <h2 className={`text-xl font-semibold ${isDark ? 'text-white' : 'text-gray-800'}`}>
            Share Chat Transcript - {customerName}
          </h2>
          <p className={`mt-2 text-sm ${isDark ? 'text-gray-300' : 'text-gray-600'}`}>
            Generate a professional chat transcript to share with the customer via email.
          </p>
        </div>

        {/* Content */}
        <div className="p-6 space-y-6">
          {/* Error Message */}
          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
              {error}
            </div>
          )}

          {/* Generate Transcript Section */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h3 className={`text-lg font-medium ${isDark ? 'text-white' : 'text-gray-800'}`}>
                Chat Transcript
              </h3>
              <button
                onClick={handleGenerateTranscript}
                disabled={isGeneratingTranscript || transcriptGenerated}
                className={`
                  ${transcriptGenerated ? 'bg-green-600 hover:bg-green-700' : themeClasses.button}
                  text-white px-4 py-2 rounded-lg text-sm transition-all duration-200
                  disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2
                `}
              >
                {isGeneratingTranscript ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    <span>Generating...</span>
                  </>
                ) : transcriptGenerated ? (
                  <>
                    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                    <span>Generated</span>
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M4 2a2 2 0 00-2 2v11a2 2 0 002 2h8a2 2 0 002-2V7.414A2 2 0 0013.414 6L10 2.586A2 2 0 008.586 2H4zm0 2h4.586L12 7.414V15H4V4z" clipRule="evenodd" />
                    </svg>
                    <span>Generate Transcript</span>
                  </>
                )}
              </button>
            </div>

            {/* Transcript Display */}
            {transcript && (
              <div className={`${isDark ? 'bg-gray-700 border-gray-600' : 'bg-gray-50 border-gray-200'} border rounded-lg p-6 space-y-6`}>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <h4 className={`font-medium mb-3 ${isDark ? 'text-white' : 'text-gray-800'} flex items-center`}>
                      <svg className="w-5 h-5 mr-2 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                      </svg>
                      Problem Reported
                    </h4>
                    <p className={`text-sm ${isDark ? 'text-gray-300' : 'text-gray-600'} bg-red-50/10 p-4 rounded border-l-4 border-red-500`}>
                      {transcript.problemReported}
                    </p>
                  </div>

                  <div>
                    <h4 className={`font-medium mb-3 ${isDark ? 'text-white' : 'text-gray-800'} flex items-center`}>
                      <svg className="w-5 h-5 mr-2 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                      </svg>
                      Solution Provided
                    </h4>
                    <p className={`text-sm ${isDark ? 'text-gray-300' : 'text-gray-600'} bg-green-50/10 p-4 rounded border-l-4 border-green-500`}>
                      {transcript.solutionProvided}
                    </p>
                  </div>
                </div>

                <div>
                  <h4 className={`font-medium mb-3 ${isDark ? 'text-white' : 'text-gray-800'} flex items-center`}>
                    <svg className="w-5 h-5 mr-2 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" clipRule="evenodd" />
                    </svg>
                    Summary
                  </h4>
                  <p className={`text-sm ${isDark ? 'text-gray-300' : 'text-gray-600'} bg-blue-50/10 p-4 rounded border-l-4 border-blue-500`}>
                    {transcript.summary}
                  </p>
                </div>

                <div className={`${isDark ? 'border-gray-600' : 'border-gray-300'} border-t pt-4`}>
                  <p className={`text-xs ${isDark ? 'text-gray-400' : 'text-gray-500'} text-center`}>
                    Resolution completed on {transcript.resolutionDate} by {transcript.agentName}
                  </p>
                </div>
              </div>
            )}
          </div>

          {/* Send Email Section */}
          {transcript && (
            <div className="space-y-4">
              <h3 className={`text-lg font-medium ${isDark ? 'text-white' : 'text-gray-800'}`}>
                Send Transcript to Customer
              </h3>
              
              <div className="space-y-3">
                <div>
                  <label htmlFor="customerEmail" className={`block text-sm font-medium mb-2 ${isDark ? 'text-gray-300' : 'text-gray-700'}`}>
                    Customer Email Address
                  </label>
                  <input
                    id="customerEmail"
                    type="email"
                    value={customerEmail}
                    onChange={handleEmailChange}
                    placeholder={`Enter email address for ${customerName}`}
                    disabled={emailSent}
                    className={`
                      w-full px-3 py-2 border rounded-lg text-sm transition-all duration-200
                      ${emailValidationError 
                        ? isDark 
                          ? 'bg-gray-700 border-red-500 text-white placeholder-gray-400 focus:border-red-500' 
                          : 'bg-white border-red-300 text-gray-900 placeholder-gray-500 focus:border-red-500'
                        : isDark 
                          ? 'bg-gray-700 border-gray-600 text-white placeholder-gray-400 focus:border-blue-500' 
                          : 'bg-white border-gray-300 text-gray-900 placeholder-gray-500 focus:border-blue-500'
                      }
                      focus:outline-none focus:ring-2 
                      ${emailValidationError ? 'focus:ring-red-500' : 'focus:ring-blue-500'} 
                      focus:ring-opacity-20 disabled:opacity-50 disabled:cursor-not-allowed
                    `}
                  />
                  {emailValidationError && (
                    <p className="mt-1 text-sm text-red-500 flex items-center">
                      <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                      </svg>
                      {emailValidationError}
                    </p>
                  )}
                </div>
                
                <div className="flex items-center justify-between">
                  <p className={`text-sm ${isDark ? 'text-gray-400' : 'text-gray-500'}`}>
                    {!isEmailValid && customerEmail.trim() 
                      ? 'Please enter a valid email address to send the transcript.'
                      : !customerEmail.trim()
                        ? 'Enter a valid email address to enable sending.'
                        : 'The structured transcript will be sent in a professional format.'
                    }
                  </p>
                  <button
                    onClick={handleSendEmail}
                    disabled={isSendingEmail || emailSent || !isEmailValid}
                    className={`
                      ${emailSent 
                        ? 'bg-green-600 hover:bg-green-700' 
                        : !isEmailValid 
                          ? 'bg-gray-400 cursor-not-allowed'
                          : themeClasses.button
                      }
                      text-white px-4 py-2 rounded-lg text-sm transition-all duration-200
                      disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2
                    `}
                  >
                    {isSendingEmail ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                        <span>Sending Email...</span>
                      </>
                    ) : emailSent ? (
                      <>
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                        </svg>
                        <span>Email Sent Successfully</span>
                      </>
                    ) : (
                      <>
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M2.94 6.412A2 2 0 002 8.108V16a2 2 0 002 2h12a2 2 0 002-2V8.108a2 2 0 00-.94-1.696l-6-3.75a2 2 0 00-2.12 0l-6 3.75zm1.06 2.196L10 5.333l6 3.75V16H4V8.608z" clipRule="evenodd" />
                        </svg>
                        <span>Send Email</span>
                      </>
                    )}
                  </button>
                </div>
                
                {/* Email sending progress */}
                {isSendingEmail && (
                  <div className={`mt-3 p-3 rounded-lg ${isDark ? 'bg-blue-900/20' : 'bg-blue-50'} border ${isDark ? 'border-blue-800' : 'border-blue-200'}`}>
                    <div className="flex items-center space-x-3">
                      <div className="w-5 h-5 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
                      <div className="flex-1">
                        <p className={`text-sm font-medium ${isDark ? 'text-blue-200' : 'text-blue-800'}`}>
                          Sending transcript email...
                        </p>
                        <p className={`text-xs ${isDark ? 'text-blue-300' : 'text-blue-600'}`}>
                          This may take a few moments to process and deliver.
                        </p>
                      </div>
                    </div>
                  </div>
                )}
                
                {/* Email sent confirmation */}
                {emailSent && (
                  <div className={`mt-3 p-3 rounded-lg ${isDark ? 'bg-green-900/20' : 'bg-green-50'} border ${isDark ? 'border-green-800' : 'border-green-200'}`}>
                    <div className="flex items-center space-x-3">
                      <svg className="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                      <div className="flex-1">
                        <p className={`text-sm font-medium ${isDark ? 'text-green-200' : 'text-green-800'}`}>
                          Email sent successfully!
                        </p>
                        <p className={`text-xs ${isDark ? 'text-green-300' : 'text-green-600'}`}>
                          The transcript has been delivered to {customerEmail}
                        </p>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className={`${isDark ? 'border-gray-700 bg-gray-750' : 'border-gray-200 bg-gray-50'} border-t p-6 flex justify-end space-x-3`}>
          <button
            onClick={handleClose}
            className={`
              px-4 py-2 border rounded-lg text-sm transition-all duration-200
              ${isDark ? 'border-gray-600 text-gray-300 hover:bg-gray-700' : 'border-gray-300 text-gray-700 hover:bg-gray-50'}
            `}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
