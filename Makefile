.PHONY: up up-telemetry down down-clean build build-release restore clean \
       migrate migration-add migrate-tilt migrate-container \
       test test-unit test-integration test-performance benchmark test-coverage \
       docker-build docker-run \
       helm-lint helm-template-dev helm-template-staging helm-template-prod \
       helm-install-dev helm-install-staging helm-install-prod \
       format format-check rabbitmq-ui health help

# ─── Development ──────────────────────────────────────────────────────────────

## Start all services via Tilt
up:
	tilt up

## Start with telemetry enabled
up-telemetry:
	tilt up -- --telemetry

## Stop all services
down:
	tilt down

## Stop and remove volumes (full reset)
down-clean:
	tilt down --delete-namespaces

# ─── Build ────────────────────────────────────────────────────────────────────

## Restore NuGet packages
restore:
	dotnet restore

## Build the solution
build:
	dotnet build

## Build in Release mode
build-release:
	dotnet build -c Release

## Clean build artifacts
clean:
	dotnet clean
	rm -rf src/ClinicApi/bin src/ClinicApi/obj
	rm -rf tests/*/bin tests/*/obj

# ─── Database ─────────────────────────────────────────────────────────────────

## Run EF Core migrations (via dotnet ef locally, or --migrate flag in container)
migrate:
	dotnet ef database update --project src/ClinicApi/ClinicApi.csproj

## Add a new migration
migration-add:
	@read -p "Migration name: " name; \
	dotnet ef migrations add $$name --project src/ClinicApi/ClinicApi.csproj --output-dir Infrastructure/Persistence/Migrations

## Trigger migration via Tilt (manual k8s Job)
migrate-tilt:
	tilt trigger 8-db-migrate

## Run migration via the container image (requires: make docker-build)
migrate-container:
	docker run --rm --env-file .env.dev clinic-api:local --migrate

# ─── Testing ──────────────────────────────────────────────────────────────────

## Run all tests
test:
	dotnet test

## Run unit tests only
test-unit:
	dotnet test tests/ClinicApi.Unit --filter "Category=Unit"

## Run integration tests only (requires Docker)
test-integration:
	dotnet test tests/ClinicApi.Integration --filter "Category=Integration"

## Run performance tests (NBomber load tests)
test-performance:
	dotnet test tests/ClinicApi.Performance --filter "Category=Performance"

## Run BenchmarkDotNet micro-benchmarks
benchmark:
	dotnet run --project tests/ClinicApi.Performance -c Release -- --filter "*"

## Run tests with coverage
test-coverage:
	dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# ─── Docker ───────────────────────────────────────────────────────────────────

## Build Docker image locally (production stage)
docker-build:
	docker build -f docker/api/Dockerfile -t clinic-api:local .

## Run Docker image locally
docker-run:
	docker run -p 5000:5000 --env-file .env.dev clinic-api:local

# ─── Helm ─────────────────────────────────────────────────────────────────────
# NOTE: helm-lint for dev may warn about empty secrets.data (populated by Tilt at runtime).
# Use helm-template-* targets for full validation, or pass --set secrets.data.SA_PASSWORD=x etc.

## Lint the Helm chart against all environments
helm-lint:
	helm lint ./helm/clinic-api -f ./helm/clinic-api/values-dev.yaml
	helm lint ./helm/clinic-api -f ./helm/clinic-api/values-staging.yaml
	helm lint ./helm/clinic-api -f ./helm/clinic-api/values-prod.yaml

## Template render (dry-run) for dev
helm-template-dev:
	helm template clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-dev.yaml

## Template render (dry-run) for staging
helm-template-staging:
	helm template clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-staging.yaml

## Template render (dry-run) for prod
helm-template-prod:
	helm template clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-prod.yaml

## Install chart (dev) — prefer 'make up' (Tilt handles secrets injection)
helm-install-dev:
	helm upgrade --install clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-dev.yaml -n clinic-api --create-namespace

## Install chart (staging)
helm-install-staging:
	helm upgrade --install clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-staging.yaml -n clinic-api-staging --create-namespace

## Install chart (prod) — requires confirmation
helm-install-prod:
	@echo "⚠️  Deploying to PRODUCTION. Press Ctrl+C to abort."
	@read -p "Continue? [y/N] " confirm && [ "$$confirm" = "y" ] || exit 1
	helm upgrade --install clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-prod.yaml -n clinic-api-prod --create-namespace

# ─── Utilities ────────────────────────────────────────────────────────────────

## Format code
format:
	dotnet format

## Check code formatting
format-check:
	dotnet format --verify-no-changes

## View RabbitMQ management UI
rabbitmq-ui:
	@echo "RabbitMQ Management: http://localhost:15672 (guest/guest)"
	@xdg-open http://localhost:15672 2>/dev/null || open http://localhost:15672 2>/dev/null || true

## Show API health
health:
	@curl -s http://localhost:5000/health | jq . 2>/dev/null || curl -s http://localhost:5000/health

## Show available targets with descriptions
help:
	@awk 'BEGIN {FS = ":"} /^## /{desc=substr($$0,4); next} /^[a-zA-Z_-]+:/{if (desc) {printf "  \033[36m%-22s\033[0m %s\n", $$1, desc; desc=""}}' $(MAKEFILE_LIST)
