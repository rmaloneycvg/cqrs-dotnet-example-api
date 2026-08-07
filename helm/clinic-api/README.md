# Helm Chart: clinic-api
This chart deploys the Clinic API stack (API, MSSQL, RabbitMQ, NGINX gateway).

## Install

```bash
# Dev (default)
helm install clinic-api ./helm/clinic-api -f helm/clinic-api/values-dev.yaml

# Staging
helm install clinic-api ./helm/clinic-api -f helm/clinic-api/values-staging.yaml -n clinic-api-staging

# Production
helm install clinic-api ./helm/clinic-api -f helm/clinic-api/values-prod.yaml -n clinic-api-prod
```

## Upgrade

```bash
helm upgrade clinic-api ./helm/clinic-api -f helm/clinic-api/values-<env>.yaml
```

## Values per environment

| File | Purpose |
|------|---------|
| `values.yaml` | Shared defaults |
| `values-dev.yaml` | Local/dev overrides (single replica, relaxed resources) |
| `values-staging.yaml` | Staging (cert-manager, restricted CORS, moderate resources) |
| `values-prod.yaml` | Production (HA replicas, strict CORS, full resources, no RabbitMQ UI) |
