import { expect, type Page, test } from '@playwright/test';
import { loginWithCas, verifyHomePageAnonymously, verifyLoginPageSchemes } from '../support/helpers';

const LOGIN_PATH = '/login';

/** Waits out the SPA proxy launch page before the React app has rendered. */
async function waitForSpaProxy(page: Page): Promise<void> {
  await page
    .waitForFunction(
      () => !document.title.includes('SPA proxy launch page') && !document.body.innerText.includes('Launching the SPA proxy'),
      { timeout: 60000 },
    )
    .catch((error: unknown) => console.warn(`Warning: waitForSpaProxy timed out or failed: ${String(error)}`));
  await page
    .waitForSelector('#root > *', { state: 'visible', timeout: 30000 })
    .catch((error: unknown) => console.warn(`Warning: waitForSpaProxy timed out or failed: ${String(error)}`));
}

async function gotoHome(page: Page): Promise<void> {
  await page.goto('/');
  await waitForSpaProxy(page);
}

async function gotoLogin(page: Page): Promise<void> {
  await gotoHome(page);
  if (page.url().endsWith(LOGIN_PATH)) {
    return;
  }
  try {
    await page.getByRole('link', { name: 'Login' }).click({ timeout: 10000 });
    await page.waitForURL(new RegExp(LOGIN_PATH), { timeout: 30000 });
  } catch {
    // The app may already be navigating (e.g. a 401 from the profile API triggered a redirect) — fall back to a direct navigation.
    await page.goto(LOGIN_PATH);
  }
}

test('home page shows anonymous message when not authenticated', async ({ page }) => {
  await gotoHome(page);
  await verifyHomePageAnonymously(page);
});

test('login page shows authentication schemes', async ({ page }) => {
  await gotoLogin(page);
  await verifyLoginPageSchemes(page);
});

test('clicking login navigates to the login page', async ({ page }) => {
  await gotoHome(page);

  // The app may already have redirected to /login on a 401 from the profile API.
  if (!page.url().includes('/login')) {
    await page
      .getByRole('link', { name: 'Login' })
      .click({ timeout: 5000 })
      .catch(() => undefined);
  }

  await expect(page).toHaveURL(/\/login/, { timeout: 30000 });
});

test('selecting CAS redirects to Keycloak', async ({ page }) => {
  await gotoLogin(page);

  const casButton = page.getByRole('button', { name: 'CAS' });
  await expect(casButton).toBeVisible({ timeout: 30000 });
  await casButton.click();

  await expect(page).toHaveURL(/auth\.dev\.local/, { timeout: 30000 });
});

test('full login flow with CAS shows user info', async ({ page, baseURL }) => {
  await gotoLogin(page);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');
  await page.waitForLoadState('networkidle');

  const heading = page.getByRole('heading', { level: 1 });
  await expect(heading).toContainText('Hello,', { timeout: 15000 });
  await expect(heading).not.toContainText('anonymous', { timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible({ timeout: 15000 });
});

test('authenticated user can see user details', async ({ page, baseURL }) => {
  await gotoLogin(page);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');
  await page.waitForLoadState('networkidle');

  await expect(page.locator("dt:text('ID')")).toBeVisible({ timeout: 15000 });
  await expect(page.locator("dt:text('Email')")).toBeVisible({ timeout: 15000 });
});

test('authenticated user can access home page', async ({ page, baseURL }) => {
  await gotoLogin(page);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');
  await page.waitForLoadState('networkidle');

  await gotoHome(page);

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello,', { timeout: 15000 });
  await expect(page.getByRole('button', { name: 'Logout' })).toBeVisible({ timeout: 15000 });
});

test('logout returns to anonymous state', async ({ page, baseURL }) => {
  await gotoLogin(page);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');
  await page.waitForLoadState('load');

  await page.getByRole('button', { name: 'Logout' }).click();

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello, anonymous', { timeout: 30000 });
});

test('authenticated user stays logged in after navigation', async ({ page, baseURL }) => {
  await gotoLogin(page);

  const loggedIn = await loginWithCas(page, baseURL!);
  test.skip(!loggedIn, 'CAS authentication scheme not available');
  await page.waitForLoadState('networkidle');

  await gotoHome(page);
  const heading = page.getByRole('heading', { level: 1 });
  // The profile fetch resolves after the initial render, so wait past the anonymous flash before reading the name.
  await expect(heading).not.toContainText('anonymous', { timeout: 15000 });
  const username = (await heading.textContent()) ?? '';

  await gotoHome(page);
  await expect(page.getByRole('heading', { level: 1 })).toContainText(username, { timeout: 15000 });
});
