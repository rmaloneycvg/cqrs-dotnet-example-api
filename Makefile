.PHONY: up down build test test-unit test-integration test-performance migrate restore clean

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

## Stop and remove volumes
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

## Run EF Core migrations
migrate:
	dotnet ef database update --project src/ClinicApi/ClinicApi.csproj

## Add a new migration
migration-add:
	@read -p "Migration name: " name; \
	dotnet ef migrations add $$name --project src/ClinicApi/ClinicApi.csproj --output-dir Infrastructure/Persistence/Migrations

## Trigger migration via Tilt
migrate-tilt:
	tilt trigger db-migrate

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

## Build Docker image locally
docker-build:
	docker build -f docker/api/Dockerfile -t clinic-api:local .

## Run Docker image locally
docker-run:
	docker run -p 5000:5000 --env-file .env.local clinic-api:local

# ─── Helm ─────────────────────────────────────────────────────────────────────

## Lint the Helm chart
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

## Install chart (dev)
helm-install-dev:
	helm upgrade --install clinic-api ./helm/clinic-api -f ./helm/clinic-api/values-dev.yaml

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

## Help
help:
	@echo "Available targets:"
	@grep -E '^## ' Makefile | sed 's/## /  /'
	@echo ""
	@grep -E '^[a-zA-Z_-]+:' Makefile | grep -v '.PHONY' | sed 's/:.*//' | sort | xargs -I {} echo "  make {}"
