import type { Locator, Page } from '@playwright/test';

export class KeycloakLoginPage {
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;

  constructor(private readonly page: Page) {
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.loginButton = page.locator('#kc-login');
  }

  async isOnLoginPage(timeout = 5000): Promise<boolean> {
    return this.usernameInput
      .waitFor({ timeout })
      .then(() => true)
      .catch(() => false);
  }

  async login(username: string, password: string): Promise<void> {
    await this.usernameInput.fill(username);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }

  /** Waits until the CAS redirect lands back on the sample app's host, regardless of port (the React sample's dev proxy can hop ports). */
  async waitForRedirect(expectedBaseUrl: string, timeout = 30000): Promise<void> {
    const host = new URL(expectedBaseUrl).hostname;
    await this.page.waitForURL((url) => url.hostname === host, { timeout });
  }
}
