FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS dotnet-lts-runtime

# https://hub.docker.com/_/microsoft-dotnet-sdk/
# Using Debian-based image for better Playwright browser support
FROM mcr.microsoft.com/dotnet/sdk:9.0

# install ASP.NET Core 8.0 Runtime
COPY --from=dotnet-lts-runtime /usr/share/dotnet /usr/share/dotnet

# Install Playwright dependencies
# https://playwright.dev/dotnet/docs/intro#system-requirements
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    libatspi2.0-0 \
    && rm -rf /var/lib/apt/lists/*