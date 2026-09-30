FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/VolksbankTracker.API/VolksbankTracker.API.csproj src/VolksbankTracker.API/
COPY src/VolksbankTracker.Core/VolksbankTracker.Core.csproj src/VolksbankTracker.Core/
RUN dotnet restore src/VolksbankTracker.API/VolksbankTracker.API.csproj -r linux-musl-x64

COPY src/ src/
RUN dotnet publish src/VolksbankTracker.API/VolksbankTracker.API.csproj \
    -c Release -r linux-musl-x64 --self-contained false --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENV ConnectionStrings__Default="Data Source=/data/tracker.db" \
    DataProtection__KeysPath=/data/keys
RUN mkdir -p /data/keys && chown -R $APP_UID:$APP_UID /data
VOLUME /data

WORKDIR /app
COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VolksbankTracker.API.dll"]
