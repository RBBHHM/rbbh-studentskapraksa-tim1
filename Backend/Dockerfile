# Build from the repository root: docker build -f Backend/Dockerfile .
FROM artifacts.rbi.tech/mcr.microsoft.com-docker-proxy/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
FROM artifacts.rbi.tech/mcr.microsoft.com-docker-proxy/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source
COPY NuGet.config ./
COPY Backend/ Backend/
RUN dotnet restore "Backend/RelatedPartiesRegister.csproj" --configfile NuGet.config
RUN dotnet publish "Backend/RelatedPartiesRegister.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RelatedPartiesRegister.dll"]
