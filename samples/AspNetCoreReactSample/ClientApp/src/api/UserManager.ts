import { User } from '../types';
import { fetchJson } from './fetchClient';

export class UserManager {
  public async isAuthenticated() {
    const user = await this.getUser();
    return !!user;
  }

  public async getUser(): Promise<User | null> {
    return await fetchJson<User>('/api/account/profile');
  }

  public async getAuthenticationSchemes(): Promise<string[]> {
    return await fetchJson<string[]>('/api/account/auth-schemes');
  }

  public async signIn(scheme: string): Promise<void> {
    const loginUrl = `/account/login?scheme=${scheme}`;
    window.location.assign(loginUrl);
  }

  public async signOut(): Promise<void> {
    const logoutUrl = '/account/logout';
    window.location.assign(logoutUrl);
  }
}
