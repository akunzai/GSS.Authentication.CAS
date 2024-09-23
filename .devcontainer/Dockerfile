FROM mcr.microsoft.com/dotnet/aspnet:6.0-bookworm-slim AS dotnet-lts-runtime
FROM docker.io/library/mono AS mono

# https://hub.docker.com/_/microsoft-dotnet-sdk/
FROM mcr.microsoft.com/dotnet/sdk:8.0

# install ASP.NET Core 6.0 Runtime
COPY --from=dotnet-lts-runtime /usr/share/dotnet /usr/share/dotnet

# install msbuild for Mono
# https://hub.docker.com/_/mono/
COPY --from=mono /etc/ca-certificates/update.d/mono-keystore /etc/ca-certificates/update.d/
COPY --from=mono /etc/mono /etc/mono
COPY --from=mono /usr/bin/cert-sync /usr/bin/
COPY --from=mono /usr/bin/mono /usr/bin/
COPY --from=mono /usr/bin/msbuild /usr/bin/
COPY --from=mono /usr/lib/mono /usr/lib/mono
COPY --from=mono /usr/lib/libmono-* /usr/lib/
COPY --from=mono /usr/lib/libMono* /usr/lib/

# synchronizes the Mono TLS certificates
# https://www.mono-project.com/docs/about-mono/releases/3.12.0/#cert-sync
RUN cert-sync /etc/ssl/certs/ca-certificates.crt