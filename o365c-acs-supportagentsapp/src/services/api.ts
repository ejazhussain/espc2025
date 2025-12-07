// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import axios, { AxiosInstance } from 'axios';
import config from '../lib/config';

class ApiService {
  private axiosInstance: AxiosInstance;

  constructor(baseURL?: string) {
    this.axiosInstance = axios.create({
      baseURL: baseURL || config.apiBaseUrl,
      timeout: 60000,
      headers: {
        'Content-Type': 'application/json',
        'ngrok-skip-browser-warning': 'true', // Skip ngrok browser warning
      },
    });
  }

  public get = (url: string) => this.axiosInstance.get(url);
  public post = (url: string, data?: any) => this.axiosInstance.post(url, data);
  public put = (url: string, data?: any) => this.axiosInstance.put(url, data);
  public delete = (url: string) => this.axiosInstance.delete(url);
  public patch = (url: string, data?: any) => this.axiosInstance.patch(url, data);

  public setAuthToken(token: string): void {
    this.axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  public removeAuthToken(): void {
    delete this.axiosInstance.defaults.headers.common['Authorization'];
  }
}

// Create and export a singleton instance
export const apiService = new ApiService();
export default ApiService;
