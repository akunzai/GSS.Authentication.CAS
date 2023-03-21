import { User } from '../types';
import { axiosFactory } from '.';

export class UserManager {
  constructor(
    private axiosInstance = axiosFactory()
  ) {}

  public async isAuthenticated() {
    const user = await this.getUser();
    return !!user;
  }

  public async getUser(): Promise<User | null> {
    const response = await this.axiosInstance.get<User>(
      '/api/account/profile'
    );
    if (response.status === 200 && response.data){
      return response.data;
    }
    return null;
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
    const logoutUrl = '/account/logout';
    window.location.assign(logoutUrl);
  }
}
