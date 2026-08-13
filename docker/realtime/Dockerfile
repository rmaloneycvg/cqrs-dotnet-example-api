# Multi-stage Dockerfile for ClinicApi.Realtime (SignalR Service)
# Stage 1: Dependencies (cache-friendly layer)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS deps
WORKDIR /src

COPY src/ClinicApi.Realtime/ClinicApi.Realtime.csproj src/ClinicApi.Realtime/
COPY src/ClinicApi/Domain/Events/DomainEvents.cs src/ClinicApi/Domain/Events/
COPY global.json Directory.Build.props ./
RUN dotnet restore src/ClinicApi.Realtime/ClinicApi.Realtime.csproj

# Stage 2: Build
FROM deps AS build
WORKDIR /src
COPY src/ClinicApi.Realtime/ src/ClinicApi.Realtime/
COPY src/ClinicApi/Domain/Events/ src/ClinicApi/Domain/Events/
RUN dotnet build src/ClinicApi.Realtime/ClinicApi.Realtime.csproj -c Release --no-restore

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish src/ClinicApi.Realtime/ClinicApi.Realtime.csproj -c Release -o /app/publish --no-build

# Stage 4: Runtime (production)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 10001 appgroup && \
    adduser --system --uid 10001 --ingroup appgroup --home /home/appuser appuser
USER 10001

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5001

ENTRYPOINT ["dotnet", "ClinicApi.Realtime.dll"]

# Stage 5: Development (hot-reload)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/ClinicApi.Realtime/ClinicApi.Realtime.csproj src/ClinicApi.Realtime/
COPY src/ClinicApi/Domain/Events/DomainEvents.cs src/ClinicApi/Domain/Events/
RUN dotnet restore src/ClinicApi.Realtime/ClinicApi.Realtime.csproj
COPY src/ src/

RUN groupadd --system --gid 10001 appgroup && \
    useradd --system --uid 10001 --gid appgroup --home-dir /home/appuser --create-home appuser
RUN chown -R 10001:10001 /src /home/appuser
USER 10001

ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_CLI_HOME=/home/appuser
ENV HOME=/home/appuser

EXPOSE 5001

ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/ClinicApi.Realtime/ClinicApi.Realtime.csproj", "--urls", "http://+:5001"]
