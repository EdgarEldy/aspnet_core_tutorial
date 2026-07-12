# syntax=docker/dockerfile:1

# --- Stage 1: build & publish -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so a source-only change doesn't invalidate
# the NuGet restore cache on every rebuild.
COPY aspnet_core_tutorial.csproj ./
RUN dotnet restore aspnet_core_tutorial.csproj

# Copy the rest of the source and publish a self-contained-less, framework-dependent build.
COPY . .
RUN dotnet publish aspnet_core_tutorial.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Stage 2: runtime ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# The mcr.microsoft.com/dotnet/aspnet:10.0 base image ships with a dedicated
# non-root "app" system user (uid/gid 1654) since .NET 8. Reuse it instead of
# creating a new one, which avoids uid/gid collisions with accounts already
# present in the base image (e.g. the built-in "ubuntu" user/group at 1000).
USER app

ENTRYPOINT ["dotnet", "aspnet_core_tutorial.dll"]
