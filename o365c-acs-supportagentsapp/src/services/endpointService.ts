// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { apiService } from './api';

let endpointUrl: string | undefined;

export const getEndpointUrl = async (): Promise<string> => {
  if (endpointUrl === undefined) {
    try {
      const response = await apiService.get('/config/getEndpoint');
      const retrievedEndpointUrl = response.data.endpointUrl;
      endpointUrl = retrievedEndpointUrl;
      return retrievedEndpointUrl;
    } catch (error) {
      throw new Error('Failed at getting environment url');
    }
  } else {
    return endpointUrl;
  }
};
