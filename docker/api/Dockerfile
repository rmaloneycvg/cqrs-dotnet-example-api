# Multi-stage Dockerfile for ClinicApi
# Stage 1: Dependencies (cache-friendly layer)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS deps
WORKDIR /src

# Copy only project files for restore (maximizes Docker layer caching)
COPY src/ClinicApi/ClinicApi.csproj src/ClinicApi/
COPY global.json Directory.Build.props ./
RUN dotnet restore src/ClinicApi/ClinicApi.csproj

# Stage 2: Build
FROM deps AS build
WORKDIR /src
COPY src/ClinicApi/ src/ClinicApi/
RUN dotnet build src/ClinicApi/ClinicApi.csproj -c Release --no-restore

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish src/ClinicApi/ClinicApi.csproj -c Release -o /app/publish --no-build

# Stage 4: Runtime (production)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security: non-root user with explicit UID/GID for k8s runAsNonRoot compatibility
RUN addgroup --system --gid 10001 appgroup && \
    adduser --system --uid 10001 --ingroup appgroup --home /home/appuser appuser
USER 10001

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

# NOTE: No HEALTHCHECK here — Kubernetes readiness/liveness/startup probes handle
# health checking in all deployment environments. For standalone docker run, use:
#   curl http://localhost:5000/health

ENTRYPOINT ["dotnet", "ClinicApi.dll"]

# Stage 5: Development (extends runtime with hot-reload support)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src

# Copy source and restore dependencies (before user switch for permissions)
COPY global.json Directory.Build.props ./
COPY src/ClinicApi/ClinicApi.csproj src/ClinicApi/
RUN dotnet restore src/ClinicApi/ClinicApi.csproj
COPY src/ src/

# Security: non-root user (matching runtime stage UID/GID for consistency)
# SDK image uses Debian with groupadd/useradd (not addgroup/adduser)
RUN groupadd --system --gid 10001 appgroup && \
    useradd --system --uid 10001 --gid appgroup --home-dir /home/appuser --create-home appuser
RUN chown -R 10001:10001 /src /home/appuser
USER 10001

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_CLI_HOME=/home/appuser
ENV HOME=/home/appuser

EXPOSE 5000

ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/ClinicApi/ClinicApi.csproj", "--urls", "http://+:5000"]
