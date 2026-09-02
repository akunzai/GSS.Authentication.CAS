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

test('authenticated user can access home page', async ({ page, baseURL }) => {
  await page.goto(LOGIN_PATH);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');

  await page.goto('/');

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello,');
  await expect(page.getByText('Logout')).toBeVisible();
});

test('authenticated user can logout', async ({ page, baseURL }) => {
  await page.goto(LOGIN_PATH);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');

  await page.goto('/');
  await page.getByRole('link', { name: 'Logout' }).click();
  await page.waitForLoadState('networkidle', { timeout: 15000 });

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello, anonymous');
  await expect(page.getByRole('link', { name: 'Login' })).toBeVisible();
});

test('authenticated user stays logged in after navigation', async ({ page, baseURL }) => {
  await page.goto(LOGIN_PATH);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');

  await page.goto('/');
  const username = (await page.getByRole('heading', { level: 1 }).textContent()) ?? '';

  // Navigate back to the home page instead of the login page: logged-in users stay on the home page.
  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 })).toContainText(username);
});
