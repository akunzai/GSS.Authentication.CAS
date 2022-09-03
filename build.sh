#!/bin/sh

dotnet build -c Release
msbuild samples/OwinSample/OwinSample.csproj -noLogo -verbosity:minimal -restore
msbuild samples/OwinSingleLogoutSample/OwinSingleLogoutSample.csproj -noLogo -verbosity:minimal -restore