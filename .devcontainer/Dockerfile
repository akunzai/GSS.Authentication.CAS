FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS dotnet-lts-runtime

# https://hub.docker.com/_/microsoft-dotnet-sdk/
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

# install ASP.NET Core 8.0 Runtime
COPY --from=dotnet-lts-runtime /usr/share/dotnet /usr/share/dotnet