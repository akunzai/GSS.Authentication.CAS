import type { Locator, Page } from '@playwright/test';

export class LoginPage {
  readonly heading: Locator;
  readonly casButton: Locator;
  readonly openIdConnectButton: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.casButton = page.locator('button, a').getByText('CAS', { exact: true });
    this.openIdConnectButton = page.locator('button, a').getByText('OpenIdConnect', { exact: true });
  }

  async selectCas(): Promise<void> {
    await this.casButton.click();
  }

  async selectOpenIdConnect(): Promise<void> {
    await this.openIdConnectButton.click();
  }

  async hasAuthenticationSchemes(timeout = 10000): Promise<boolean> {
    await this.casButton
      .or(this.openIdConnectButton)
      .waitFor({ state: 'visible', timeout })
      .catch(() => undefined);
    return (await this.casButton.isVisible()) || (await this.openIdConnectButton.isVisible());
  }
}
