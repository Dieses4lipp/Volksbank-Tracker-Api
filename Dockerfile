# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore first: layer is only rebuilt when a project file changes
COPY src/VolksbankTracker.API/VolksbankTracker.API.csproj src/VolksbankTracker.API/
COPY src/VolksbankTracker.Core/VolksbankTracker.Core.csproj src/VolksbankTracker.Core/
RUN dotnet restore src/VolksbankTracker.API/VolksbankTracker.API.csproj -r linux-musl-x64

COPY src/ src/
RUN dotnet publish src/VolksbankTracker.API/VolksbankTracker.API.csproj \
    -c Release -r linux-musl-x64 --self-contained false --no-restore -o /app

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

# The alpine image runs globalization-invariant by default. libfintx references
# CultureInfo, so ship ICU instead of risking culture lookups failing at sync time.
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# SQLite database and Data Protection keys share one volume. The keys decrypt the
# stored FinTS credentials, so losing /data means re-entering them.
ENV ConnectionStrings__Default="Data Source=/data/tracker.db" \
    DataProtection__KeysPath=/data/keys
RUN mkdir -p /data/keys && chown -R $APP_UID:$APP_UID /data
VOLUME /data

WORKDIR /app
COPY --from=build /app .

# Non-root user from the base image (UID 1654), listens on 8080 (ASPNETCORE_HTTP_PORTS)
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VolksbankTracker.API.dll"]
