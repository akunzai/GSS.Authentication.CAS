import { User } from '../types';
import { axiosFactory } from '.';

export class UserManager {
  constructor(
    private userStoreKey = 'user:me',
    private axiosInstance = axiosFactory()
  ) {}

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
      const response = await this.axiosInstance.get<User>(
        '/api/account/profile'
      );
      await this.storeUser(response.data);
      return response.data;
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
    const response = await this.axiosInstance.get<string[]>(
      '/api/account/auth-schemes'
    );
    return response.data;
  }

  public async signIn(scheme: string): Promise<void> {
    const loginUrl = `/account/login?scheme=${scheme}`;
    window.location.assign(loginUrl);
  }

  public async signOut(): Promise<void> {
    await this.removeUser();
    const logoutUrl = '/account/logout';
    window.location.assign(logoutUrl);
  }
}
