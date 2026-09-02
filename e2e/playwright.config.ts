import { defineConfig, devices } from '@playwright/test';

const SAMPLE_BASE_URL = process.env.SAMPLE_BASE_URL;

export default defineConfig({
  testDir: 'tests',
  // Login flows share one Keycloak realm and one dev sample server per project;
  // keep tests within a spec file sequential to avoid session/dev-server contention.
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['html', { open: 'never' }], ['github']] : 'list',
  use: {
    ...devices['Desktop Chrome'],
    ignoreHTTPSErrors: true,
    trace: process.env.PLAYWRIGHT_TRACE === 'true' ? 'on' : 'retain-on-failure',
    video: process.env.PLAYWRIGHT_VIDEO === 'true' ? 'on' : 'off',
  },
  projects: [
    {
      name: 'basic',
      testMatch: 'aspnetcore.spec.ts',
      use: { baseURL: SAMPLE_BASE_URL ?? 'https://localhost:5001' },
    },
    {
      name: 'mvc',
      testMatch: 'aspnetcore-mvc.spec.ts',
      use: { baseURL: SAMPLE_BASE_URL ?? 'https://localhost:5002' },
    },
    {
      name: 'blazor',
      testMatch: 'blazor.spec.ts',
      use: { baseURL: SAMPLE_BASE_URL ?? 'https://localhost:5003' },
    },
    {
      name: 'identity',
      testMatch: 'aspnetcore-identity.spec.ts',
      use: { baseURL: SAMPLE_BASE_URL ?? 'https://localhost:5004' },
    },
    {
      name: 'react',
      testMatch: 'aspnetcore-react.spec.ts',
      use: { baseURL: SAMPLE_BASE_URL ?? 'https://localhost:5005' },
    },
  ],
});
