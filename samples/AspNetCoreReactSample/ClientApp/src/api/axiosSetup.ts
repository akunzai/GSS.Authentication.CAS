import axios, { AxiosRequestConfig, AxiosInstance, AxiosError } from 'axios';

export const axiosRequestConfig: AxiosRequestConfig = {
  responseType: 'json',
  headers: {
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
  },
};

export function axiosFactory(config: AxiosRequestConfig = axiosRequestConfig): AxiosInstance {
  const axiosInstance = axios.create(config);
  axiosInstance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      if (error.response?.status === 401) {
        window.location.assign('/login');
      }
      return Promise.reject(error);
    }
  );
  return axiosInstance;
}
