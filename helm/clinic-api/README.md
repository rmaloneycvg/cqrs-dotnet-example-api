# Helm Chart: clinic-api

Deploys the Clinic API stack: API, MSSQL, RabbitMQ, NGINX gateway, security rings.

## Architecture

```
nginx-gateway (8080/8443) → clinic-api (5000) → mssql-service (1433)
                                              → rabbitmq-service (5672)
```

- **NGINX** — TLS termination, CORS, rate limiting, reverse proxy
- **Clinic API** — .NET 10, Wolverine CQRS, EF Core + Dapper
- **MSSQL** — SQL Server 2022 (in-cluster for dev/staging, managed for prod)
- **RabbitMQ** — Message broker (in-cluster for dev/staging, managed for prod)

## Templates

| Template | Resources | Notes |
|----------|-----------|-------|
| `namespace.yaml` | Namespace + PSA labels | Pod Security Admission per env |
| `secrets.yaml` | Secret (`clinic-secrets`) | Only created when `secrets.create: true` |
| `clinic-api.yaml` | Deployment + Service | Main API workload |
| `nginx.yaml` | Deployment + Service | Non-root, listens 8080/8443 |
| `nginx-configmap.yaml` | ConfigMap | Full nginx.conf with CORS, rate limiting |
| `mssql.yaml` | Deployment + Service + PVC | Conditional on `mssql.enabled` |
| `rabbitmq.yaml` | Deployment + Service + ConfigMap + PVC | Conditional on `rabbitmq.enabled` |
| `db-migrate-job.yaml` | Job | EF Core migration, Helm hook or manual |
| `certificate.yaml` | Certificate (cert-manager) | Only when TLS mode is `cert-manager` |
| `hpa.yaml` | HorizontalPodAutoscaler | Only when `autoscaling.enabled: true` |
| `pdb.yaml` | PodDisruptionBudget | Only when `replicaCount > 1` |
| `ring2-istio-gateway.yaml` | Gateway + VirtualService + RequestAuth | Istio ingress |
| `ring2-network-policies.yaml` | NetworkPolicies | Default deny + allow rules |
| `ring3-service-mesh.yaml` | PeerAuth + AuthorizationPolicy | mTLS + L7 authz |
| `ring4-workload.yaml` | ServiceAccount + Role + RoleBinding | RBAC least privilege |
| `_helpers.tpl` | Template helpers | Labels, selectors, namespace |
| `NOTES.txt` | Post-install instructions | Access URLs per service type |

## Install

```bash
# Dev (via Tilt — preferred, handles secrets injection)
tilt up

# Dev (direct helm — must pass secrets manually)
helm install clinic-api . -f values-dev.yaml -n clinic-api \
  --set secrets.data.SA_PASSWORD=YourPass \
  --set secrets.data.RABBITMQ_DEFAULT_PASS=guest \
  --set-string 'secrets.data.ConnectionStrings__ClinicDb=Server=mssql-service\,1433;...'

# Staging
helm upgrade --install clinic-api . -f values-staging.yaml -n clinic-api-staging

# Production
helm upgrade --install clinic-api . -f values-prod.yaml -n clinic-api-prod
```

## Values

| File | Purpose |
|------|---------|
| `values.yaml` | Shared defaults (production-safe baseline) |
| `values-dev.yaml` | Local dev: ephemeral, self-signed TLS, relaxed resources, Tilt |
| `values-staging.yaml` | Staging: cert-manager, persistent storage, 2 replicas, Istio |
| `values-prod.yaml` | Production: HA, managed services, strict CORS, full security rings |

## Key Design Decisions

- **NGINX on high ports (8080/8443)** — Non-root container (UID 101). Service maps 80/443 → 8080/8443.
- **RabbitMQ topology via Wolverine** — No `load_definitions`. User created by env vars, topology auto-provisioned by application on connect.
- **Self-signed TLS baked into image** — Dev only. Cert generated at Docker build time. Staging/prod use cert-manager with Secret volume mount.
- **Secrets from `.env.dev`** — Tilt reads the file, escapes commas, injects via `helm --set`. Never committed.
- **Security rings toggled per env** — 5 concentric layers, each independently enabled. Dev gets RBAC + NetworkPolicies only.

## Linting

```bash
helm lint . -f values-dev.yaml
helm lint . -f values-staging.yaml
helm lint . -f values-prod.yaml
```

## Template Rendering (dry-run)

```bash
helm template clinic-api . -f values-dev.yaml
helm template clinic-api . -f values-staging.yaml
helm template clinic-api . -f values-prod.yaml
```
