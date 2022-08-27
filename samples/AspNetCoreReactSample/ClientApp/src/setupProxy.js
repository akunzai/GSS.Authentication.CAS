/* eslint-disable @typescript-eslint/no-var-requires */
// This script configures a proxy of the SPA Dev server and forward API request to the ASP.NET Core Host
// https://create-react-app.dev/docs/proxying-api-requests-in-development/

const { createProxyMiddleware } = require('http-proxy-middleware');
const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(';')[0]
  : 'http://localhost:5000';

const context = ['/api/**', '/account/*', '/signin-*', 'signout-*'];

module.exports = function (app) {
  const appProxy = createProxyMiddleware(context, {
    target: target,
    secure: false,
    headers: {
      Connection: 'Keep-Alive',
    },
  });

  app.use(appProxy);
};
