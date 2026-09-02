import { expect, test } from '@playwright/test';
import { loginWithCas, verifyHomePageAnonymously, verifyLoginPageSchemes } from '../support/helpers';
import { LoginPage } from '../support/login-page';

const LOGIN_PATH = '/Account/Login';

test('home page shows anonymous message when not authenticated', async ({ page }) => {
  await page.goto('/');
  await verifyHomePageAnonymously(page);
});

test('login page shows authentication schemes', async ({ page }) => {
  await page.goto(LOGIN_PATH);
  await verifyLoginPageSchemes(page);
});

test('login page redirects to Keycloak when CAS is selected', async ({ page }) => {
  await page.goto(LOGIN_PATH);
  await new LoginPage(page).selectCas();
  await expect(page).toHaveURL(/auth\.dev\.local/);
});

test('full login flow with CAS shows user info', async ({ page, baseURL }) => {
  await page.goto(LOGIN_PATH);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello,');
  await expect(page.getByText('Logout')).toBeVisible();
});
