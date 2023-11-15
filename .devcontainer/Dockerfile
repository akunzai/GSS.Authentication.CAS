# https://hub.docker.com/_/microsoft-dotnet-sdk/
FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG APT_URL=""

# install ASP.NET Core 6.0 Runtime
COPY --from=mcr.microsoft.com/dotnet/aspnet:6.0-bookworm-slim /usr/share/dotnet /usr/share/dotnet

RUN set -eux; \
    # preferred mirror site
    [ -n "$APT_URL" ] && [ "$APT_URL" = 'http://free.nchc.org.tw' ] && [ $(uname -m) = 'x86_64' ] && [ -e "/etc/apt/sources.list.d/debian.sources" ] && sed -i -E "/-security/! s,URIs: http?://[^\/]+,URIs: $APT_URL,g" /etc/apt/sources.list.d/debian.sources; \
    [ -n "$APT_URL" ] && [ "$APT_URL" != 'http://free.nchc.org.tw' ] && [ -e "/etc/apt/sources.list.d/debian.sources" ] && sed -i -E "s,URIs: http?://[^\/]+,URIs: $APT_URL,g" /etc/apt/sources.list.d/debian.sources; \
    # install msbuild for Mono
    # https://www.mono-project.com/download/stable/#download-lin-debian
    apt-get update; \
    apt-get install -y --no-install-recommends dirmngr gnupg ca-certificates; \
    # avoid connection timeout
    timeout 10 apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF || apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF; \
    echo "deb http://download.mono-project.com/repo/debian stable-buster main" > /etc/apt/sources.list.d/mono-official-stable.list; \
    apt-get update; \
    apt-get install -y mono-devel; \
    # clean up
    apt-get purge -y --auto-remove gnupg dirmngr; \
    rm -rf /var/lib/apt/lists/* /tmp/*; \
    # smoke test
    dotnet --info && msbuild -version;