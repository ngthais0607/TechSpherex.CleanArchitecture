import { environment } from '../../../environments/environment';

export const API_BASE_URL = environment.apiBaseUrl;
export function isApiUrl(url: string): boolean {
  try {
    const resolved = new URL(url, location.origin);
    return resolved.origin === location.origin &&
      (resolved.pathname === API_BASE_URL || resolved.pathname.startsWith(API_BASE_URL + '/'));
  } catch { return false; }
}
