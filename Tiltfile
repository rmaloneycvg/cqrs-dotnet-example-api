# Tiltfile for CQRS .NET Example API
# Orchestrates: NGINX, MSSQL, RabbitMQ, ClinicApi with live-reload
# Single source of truth: helm/clinic-api/ chart with values-dev.yaml

# ─── Safety ──────────────────────────────────────────────────────────────────

# Only allow running against local clusters — prevents accidental prod deploy
allow_k8s_contexts([
    'docker-desktop',
    'minikube',
    'kind-kind',
    'rancher-desktop',
    'colima',
    'k3d-k3s-default',
    'orbstack',
])

# ─── Configuration ───────────────────────────────────────────────────────────

config.define_bool("telemetry")
cfg = config.parse()
enable_telemetry = cfg.get("telemetry", False)

update_settings(
    max_parallel_updates=3,
    k8s_upsert_timeout_secs=120,
    suppress_unused_image_warnings=['nginx-gateway-image'],
)

# ─── Helm Chart (single source of k8s manifests) ─────────────────────────────

k8s_yaml(helm(
    './helm/clinic-api',
    name='clinic-api',
    namespace='clinic-api',
    values=['./helm/clinic-api/values-dev.yaml'],
))

# ─── Docker Builds ───────────────────────────────────────────────────────────

# NGINX Gateway
# Self-signed TLS cert generated at build time. Config injected via ConfigMap,
# so no live_update needed — only rebuilds on Dockerfile changes.
docker_build(
    'nginx-gateway-image',
    context='./docker/nginx',
    dockerfile='docker/nginx/Dockerfile',
    ignore=['README.md'],
)

# Clinic API with live_update for hot reload via `dotnet watch`
docker_build(
    'clinic-api-image',
    context='.',
    dockerfile='docker/api/Dockerfile',
    target='development',
    ignore=[
        'tests/',
        'docs/',
        'helm/',
        '*.md',
        'Makefile',
        'Tiltfile',
        '.git/',
        'coverage/',
    ],
    live_update=[
        # Full rebuild if project file or build props change
        fall_back_on([
            './src/ClinicApi/ClinicApi.csproj',
            './global.json',
            './Directory.Build.props',
        ]),
        # Sync source for dotnet watch hot-reload
        sync('./src', '/src/src'),
    ],
)

# ─── Resource Configuration ──────────────────────────────────────────────────
# Resources are numbered to show startup/workflow order in the Tilt UI.

# 1. Namespace & RBAC (must exist before anything else)
k8s_resource(
    objects=['clinic-api:namespace'],
    new_name='1-namespace',
    labels=['infra'],
)

k8s_resource(
    objects=[
        'clinic-api:serviceaccount',
        'clinic-api-role:role',
        'clinic-api-rolebinding:rolebinding',
    ],
    new_name='2-rbac',
    labels=['infra'],
)

k8s_resource(
    objects=['clinic-secrets:secret'],
    new_name='3-secrets',
    labels=['infra'],
)

# 4-5. Infrastructure (databases & messaging — no deps on app)
k8s_resource(
    'mssql',
    new_name='4-mssql',
    labels=['infra'],
    port_forwards=['1433:1433'],
)

k8s_resource(
    'rabbitmq',
    new_name='5-rabbitmq',
    labels=['infra'],
    port_forwards=[
        port_forward(5672, 5672, name='AMQP'),
        port_forward(15672, 15672, name='Management UI'),
    ],
    links=[
        link('http://localhost:15672', 'RabbitMQ Management (guest/guest)'),
    ],
    objects=['rabbitmq-config:configmap'],
)

# 6. Application (depends on infra)
k8s_resource(
    'clinic-api',
    new_name='6-clinic-api',
    labels=['app'],
    port_forwards=[
        port_forward(5000, 5000, name='API'),
    ],
    resource_deps=['4-mssql', '5-rabbitmq'],
    links=[
        link('http://localhost:5000/health', 'Health Check'),
        link('http://localhost:5000/swagger', 'Swagger UI'),
    ],
)

# 7. Gateway (depends on API being up)
k8s_resource(
    'nginx-gateway',
    new_name='7-nginx-gateway',
    labels=['gateway'],
    port_forwards=[
        port_forward(8443, 443, name='HTTPS'),
        port_forward(8080, 80, name='HTTP'),
    ],
    resource_deps=['6-clinic-api'],
    objects=['nginx-config:configmap'],
    links=[
        link('https://localhost:8443', 'Gateway (HTTPS)'),
    ],
)

# 8. Database Migration (manual trigger — run after infra is healthy)
k8s_resource(
    'db-migrate-manual',
    new_name='8-db-migrate',
    labels=['tooling'],
    resource_deps=['4-mssql'],
    auto_init=False,
    trigger_mode=TRIGGER_MODE_MANUAL,
)

# ─── Telemetry (optional) ────────────────────────────────────────────────────

if enable_telemetry:
    print("📊 Telemetry enabled — TODO: add OpenTelemetry collector, Jaeger, Prometheus")
    # Future: load telemetry manifests here
    # k8s_yaml('./k8s/telemetry/')
    # k8s_resource('otel-collector', labels=['telemetry'], ...)
    # k8s_resource('jaeger', labels=['telemetry'], ...)

# ─── Resource Groups ─────────────────────────────────────────────────────────
# Tilt UI groups (numbered by startup order):
#   infra     → 1-namespace, 2-rbac, 3-secrets, 4-mssql, 5-rabbitmq
#   app       → 6-clinic-api
#   gateway   → 7-nginx-gateway
#   tooling   → 8-db-migrate (manual)
#   telemetry → (future: otel-collector, jaeger, prometheus, grafana)
