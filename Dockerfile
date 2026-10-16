FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Domain/SnpDepotManagement.Domain.csproj src/Domain/
COPY src/Application/SnpDepotManagement.Application.csproj src/Application/
COPY src/Infrastructure/SnpDepotManagement.Infrastructure.csproj src/Infrastructure/
COPY src/Api/SnpDepotManagement.Api.csproj src/Api/
RUN dotnet restore src/Api/SnpDepotManagement.Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/SnpDepotManagement.Api.csproj -c Release --no-restore -o /out/api /p:UseAppHost=false

FROM build AS bundle
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.12
# Design-time model compilation only; no connection is opened during bundle creation.
RUN ConnectionStrings__DefaultConnection='Host=localhost;Database=bundle_build;Username=placeholder;Password=placeholder' /tools/dotnet-ef migrations bundle --project src/Infrastructure --startup-project src/Api --configuration Release -r linux-x64 -o /out/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
USER root
RUN apt-get update && apt-get install -y --no-install-recommends postgresql-client && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=bundle /out/efbundle ./efbundle
COPY scripts/release-db.sh scripts/apply-user-tenants.sql ./scripts/
RUN chmod 755 /app/efbundle /app/scripts/release-db.sh
USER $APP_UID
ENTRYPOINT ["sh", "/app/scripts/release-db.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/api/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 CMD curl --fail --silent http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "SnpDepotManagement.Api.dll"]
