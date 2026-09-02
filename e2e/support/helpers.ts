import { expect, type Page } from '@playwright/test';
import { KeycloakLoginPage } from './keycloak-login-page';
import { LoginPage } from './login-page';

export const TEST_USERNAME = 'test';
export const TEST_PASSWORD = 'test';

/** Selects the CAS scheme and completes the Keycloak login when it's offered. Returns false when CAS isn't a configured scheme. */
export async function loginWithCas(page: Page, baseURL: string): Promise<boolean> {
  const loginPage = new LoginPage(page);
  // The React sample fetches its scheme list asynchronously, so the button can render after navigation settles.
  await loginPage.casButton.waitFor({ state: 'visible', timeout: 10000 }).catch(() => undefined);
  if (!(await loginPage.casButton.isVisible())) {
    return false;
  }
  await loginPage.selectCas();

  const keycloakPage = new KeycloakLoginPage(page);
  if (await keycloakPage.isOnLoginPage()) {
    await keycloakPage.login(TEST_USERNAME, TEST_PASSWORD);
    await keycloakPage.waitForRedirect(baseURL);
  }
  return true;
}

export async function verifyHomePageAnonymously(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello, anonymous', { timeout: 30000 });
  await expect(page.getByRole('link', { name: 'Login' })).toBeVisible({ timeout: 30000 });
}

export async function verifyLoginPageSchemes(page: Page): Promise<void> {
  const loginPage = new LoginPage(page);
  await expect(loginPage.heading).toContainText('Choose an authentication scheme');
  expect(await loginPage.hasAuthenticationSchemes()).toBe(true);
}
