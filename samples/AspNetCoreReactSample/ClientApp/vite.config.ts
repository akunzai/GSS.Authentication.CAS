import react from '@vitejs/plugin-react';
import { existsSync, readFileSync } from 'fs';
import { defineConfig, loadEnv } from 'vite';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  // Load env file based on `mode` in the current working directory.
  // Set the third parameter to '' to load all env regardless of the `VITE_` prefix.
  const env = loadEnv(mode, process.cwd(), '');
  const certFilePath = env.SSL_CRT_FILE;
  const keyFilePath = env.SSL_KEY_FILE;
  const httpsOptions =
    env.HTTPS && existsSync(certFilePath) && existsSync(keyFilePath)
      ? {
          cert: readFileSync(certFilePath),
          key: readFileSync(keyFilePath),
        }
      : false;
  const proxyTarget = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(';')[0]
  : 'http://localhost:5000';
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
