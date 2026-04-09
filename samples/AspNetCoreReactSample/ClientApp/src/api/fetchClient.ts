export interface FetchOptions extends RequestInit {
  headers?: HeadersInit;
}

export const defaultOptions: FetchOptions = {
  headers: {
    'X-Requested-With': 'XMLHttpRequest',
  },
};

export async function fetchJson<T>(
  url: string,
  options: FetchOptions = defaultOptions
): Promise<T> {
  const headers = new Headers(defaultOptions.headers);

  if (options && options.headers) {
    const customHeaders = new Headers(options.headers);
    customHeaders.forEach((value, key) => headers.set(key, value));
  }

  if (options && options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const mergedOptions: RequestInit = {
    ...defaultOptions,
    ...options,
    headers,
  };

  const response = await fetch(url, mergedOptions);

  if (response.status === 401) {
    window.location.assign('/login');
    // Reject properly so the caller knows it failed even if we are redirecting
    return Promise.reject(new Error('Unauthorized'));
  }

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || response.statusText);
  }

  const text = await response.text();
  if (!text) {
    return null as T;
  }

  return JSON.parse(text) as T;
}
