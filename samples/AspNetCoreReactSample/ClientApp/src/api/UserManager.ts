import { FETCH_COMMON_OPTIONS } from '../constants';
import { User } from '../types';

export class UserManager {
  constructor(private userStoreKey = 'user:me') {}

  public async isAuthenticated() {
    const user = await this.getUser();
    return !!user;
  }

  public async getUser(force = false): Promise<User | null> {
    const json = sessionStorage.getItem(this.userStoreKey);
    if (json) {
      return JSON.parse(json);
    }
    if (force) {
      const response = await fetch('/api/account/profile', FETCH_COMMON_OPTIONS);
      if (response.status === 200) {
        const user = await response.json();
        await this.storeUser(user);
        return user;
      }
    }
    return null;
  }

  public async storeUser(user: User | null): Promise<void> {
    if (user) {
      const json = JSON.stringify(user);
      sessionStorage.setItem(this.userStoreKey, json);
    } else {
      sessionStorage.removeItem(this.userStoreKey);
    }
  }

  public async removeUser(): Promise<void> {
    await this.storeUser(null);
  }

  public async getAuthenticationSchemes(): Promise<string[]> {
    const response = await fetch('/api/account/auth-schemes', FETCH_COMMON_OPTIONS);
    return await response.json();
  }

  public async signIn(scheme: string): Promise<void> {
    const loginUrl = `/api/account/login?scheme=${scheme}`;
    window.location.assign(loginUrl);
  }

  public async signOut(): Promise<void> {
    await this.removeUser();
    const logoutUrl = '/api/account/logout';
    window.location.assign(logoutUrl);
  }
}
