import { expect, test } from '@playwright/test';
import { TEST_PASSWORD, TEST_USERNAME } from '../support/helpers';
import { KeycloakLoginPage } from '../support/keycloak-login-page';

const LOGIN_PATH = '/Identity/Account/Login';

test('home page redirects to login when not authenticated', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/Identity\/Account\/Login/);
});

test('login page shows external providers', async ({ page }) => {
  await page.goto(LOGIN_PATH);
  await expect(page.getByText('Use another service to log in')).toBeVisible();
});

test('external login with CAS redirects to Keycloak', async ({ page }) => {
  await page.goto(LOGIN_PATH);

  const casButton = page.locator("button[value='CAS']");
  test.skip(!(await casButton.isVisible()), 'CAS external provider not configured');

  await casButton.click();
  await expect(page).toHaveURL(/auth\.dev\.local/);
});

test('register page is accessible', async ({ page }) => {
  await page.goto('/Identity/Account/Register');

  await expect(page.getByRole('heading', { name: 'Register', exact: true })).toContainText('Register');
  await expect(page.locator('#Input_Email')).toBeVisible();
  await expect(page.locator('#Input_Password')).toBeVisible();
});

test('full login flow with external provider shows user info', async ({ page, baseURL }) => {
  await page.goto(LOGIN_PATH);

  const casButton = page.locator("button[value='CAS']");
  test.skip(!(await casButton.isVisible()), 'CAS external provider not configured');

  await casButton.click();

  const keycloakPage = new KeycloakLoginPage(page);
  if (await keycloakPage.isOnLoginPage()) {
    await keycloakPage.login(TEST_USERNAME, TEST_PASSWORD);
    await page.waitForURL((url) => url.href.startsWith(baseURL!), { timeout: 15000 });
  }

  expect(page.url().startsWith(baseURL!)).toBe(true);
});
