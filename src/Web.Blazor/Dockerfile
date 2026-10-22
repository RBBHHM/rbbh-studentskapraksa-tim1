FROM artifacts.rbi.tech/mcr.microsoft.com-docker-proxy/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM artifacts.rbi.tech/mcr.microsoft.com-docker-proxy/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["NuGet.config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["RBBH.RelatedPartiesRegister.Web.csproj", "."]
RUN dotnet restore "RBBH.RelatedPartiesRegister.Web.csproj" --configfile "NuGet.config"

COPY . .
RUN dotnet publish "RBBH.RelatedPartiesRegister.Web.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RBBH.RelatedPartiesRegister.Web.dll"]
