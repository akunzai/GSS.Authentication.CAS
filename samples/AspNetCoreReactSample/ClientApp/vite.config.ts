import react from '@vitejs/plugin-react';
import { existsSync, readFileSync } from 'fs';
import { defineConfig, loadEnv } from 'vite';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  // Load env file based on `mode` in the current working directory.
  // Set the third parameter to '' to load all env regardless of the `VITE_` prefix.
  const env = { ...process.env, ...loadEnv(mode, process.cwd(), '') };
  const certFilePath = env.SSL_CRT_FILE;
  const keyFilePath = env.SSL_KEY_FILE;
  const httpsOptions =
    env.HTTPS && certFilePath && keyFilePath && existsSync(certFilePath) && existsSync(keyFilePath)
      ? {
        cert: readFileSync(certFilePath),
        key: readFileSync(keyFilePath),
      }
      : undefined;
  const proxyTarget = env.PROXY_TARGET
    ? env.PROXY_TARGET
    : env.ASPNETCORE_URLS
      ? env.ASPNETCORE_URLS.split(';').find(url => url.startsWith('https')) ??
      env.ASPNETCORE_URLS.split(';').find(url => url.startsWith('http')) ??
      env.ASPNETCORE_URLS.split(';')[0]
      : env.ASPNETCORE_HTTPS_PORT
        ? `https://127.0.0.1:${env.ASPNETCORE_HTTPS_PORT}`
        : 'http://127.0.0.1:5000';
  // https://github.com/http-party/node-http-proxy#options
  const proxyOptions = {
    target: proxyTarget,
    secure: false
  };
  return {
    plugins: [react()],
    server: {
      port: Number(env.PORT) > 0 ? Number(env.PORT) : 3000,
      https: httpsOptions,
      // forward API request to the ASP.NET Core Host
      proxy: {
        '/api': proxyOptions,
        '/account': proxyOptions,
        '^/sign(in|out)-.+': proxyOptions
      }
    },
  };
});
