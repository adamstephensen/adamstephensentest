import ax from 'axios';
import { errorHandler } from './errorHandler';
import { msalScopes, pca } from '@/msal-configs';

const axios = ax.create({});

// Add a request interceptor
axios.interceptors.request.use(
  async (config) => {
    // Get the access token
    const accounts = pca.getAllAccounts();
    if (accounts.length > 0) {
      const response = await pca.acquireTokenSilent({
        scopes: msalScopes,
        account: accounts[0],
      });

      // Set the access token in the headers
      config.headers!['Authorization'] = `Bearer ${response.accessToken}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Request interceptor
axios.interceptors.request.use(
  (config) => {
    return config;
  },
  (error) => {
    errorHandler.handleError(error);
    return Promise.reject(error);
  }
);

// Response interceptor
axios.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status || 500;
    const context = {
      componentName: 'API_Call',
      action: error.config?.url,
      additionalData: {
        method: error.config?.method,
        url: error.config?.url,
        params: error.config?.params,
      },
    };

    const appError = errorHandler.createHttpError(status, context);
    errorHandler.handleError(appError);
    return Promise.reject(appError);
  }
);

export default axios;
