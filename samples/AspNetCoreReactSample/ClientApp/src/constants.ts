export const FETCH_COMMON_OPTIONS: RequestInit = {
  headers: new Headers({
    Accept: 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
  }),
};
