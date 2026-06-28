FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EnterprisePlatform.sln ./
COPY src/EnterprisePlatform.Core/EnterprisePlatform.Core.csproj src/EnterprisePlatform.Core/
COPY src/EnterprisePlatform.Utils/EnterprisePlatform.Utils.csproj src/EnterprisePlatform.Utils/
COPY src/EnterprisePlatform.Repository/EnterprisePlatform.Repository.csproj src/EnterprisePlatform.Repository/
COPY src/EnterprisePlatform.Service/EnterprisePlatform.Service.csproj src/EnterprisePlatform.Service/
COPY src/EnterprisePlatform.Api/EnterprisePlatform.Api.csproj src/EnterprisePlatform.Api/

RUN dotnet restore src/EnterprisePlatform.Api/EnterprisePlatform.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/EnterprisePlatform.Api/EnterprisePlatform.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EnterprisePlatform.Api.dll"]
