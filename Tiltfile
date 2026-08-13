# Tiltfile for CQRS .NET Example API
# Orchestrates: NGINX Gateway, MSSQL, RabbitMQ, ClinicApi with live-reload
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

# ─── Secrets (loaded from .env.dev) ──────────────────────────────────────────
# Secrets are loaded from .env.dev (gitignored) and injected into Helm values.
# Copy .env.example to .env.dev and fill in values for local development.

def read_env_file(path):
    """Parse a .env file into a dict, skipping comments and empty lines."""
    result = {}
    for line in str(read_file(path)).strip().split('\n'):
        line = line.strip()
        if line and not line.startswith('#'):
            key, _, value = line.partition('=')
            if key and value:
                result[key.strip()] = value.strip()
    return result

_secrets = read_env_file('.env.dev')

# ─── Helm Chart (single source of k8s manifests) ─────────────────────────────

# Helm --set treats commas as list separators. Connection strings contain commas
# (e.g., "Server=host,1433") so they must be escaped for Helm to pass them through.
def helm_escape(value):
    """Escape special characters for Helm --set values."""
    return value.replace(',', '\\,')

k8s_yaml(helm(
    './helm/clinic-api',
    name='clinic-api',
    namespace='clinic-api',
    values=['./helm/clinic-api/values-dev.yaml'],
    set=[
        'secrets.data.SA_PASSWORD=' + helm_escape(_secrets.get('SA_PASSWORD', '')),
        'secrets.data.RABBITMQ_DEFAULT_PASS=' + helm_escape(_secrets.get('RABBITMQ_DEFAULT_PASS', '')),
        'secrets.data.REDIS_PASSWORD=' + helm_escape(_secrets.get('REDIS_PASSWORD', '')),
        'secrets.data.PHI_ENCRYPTION_KEY=' + helm_escape(_secrets.get('PHI_ENCRYPTION_KEY', '')),
        'secrets.data.ConnectionStrings__ClinicDb=' + helm_escape(_secrets.get('ConnectionStrings__ClinicDb', '')),
        'secrets.data.ConnectionStrings__RabbitMq=' + helm_escape(_secrets.get('ConnectionStrings__RabbitMq', '')),
        'secrets.data.ConnectionStrings__Redis=' + helm_escape(_secrets.get('ConnectionStrings__Redis', '')),
    ],
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
    only=[
        'docker/api/',
        'src/ClinicApi/',
        'global.json',
        'Directory.Build.props',
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

# SignalR Realtime Service with live_update
docker_build(
    'signalr-service-image',
    context='.',
    dockerfile='docker/realtime/Dockerfile',
    target='development',
    only=[
        'docker/realtime/',
        'src/ClinicApi.Realtime/',
        'src/ClinicApi/Domain/Events/',
        'global.json',
        'Directory.Build.props',
    ],
    live_update=[
        fall_back_on([
            './src/ClinicApi.Realtime/ClinicApi.Realtime.csproj',
            './global.json',
            './Directory.Build.props',
        ]),
        sync('./src', '/src/src'),
    ],
)

# React Web App with live_update
docker_build(
    'web-app-image',
    context='.',
    dockerfile='docker/web/Dockerfile',
    target='development',
    only=[
        'docker/web/',
        'web/',
    ],
    live_update=[
        fall_back_on(['./web/package.json', './web/package-lock.json']),
        sync('./web', '/app'),
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

k8s_resource(
    objects=[
        'default-deny-all:networkpolicy',
        'allow-ingress-to-nginx-gateway:networkpolicy',
        'allow-nginx-gateway-to-api:networkpolicy',
        'allow-api-to-mssql:networkpolicy',
        'allow-api-to-rabbitmq:networkpolicy',
        'allow-services-to-redis:networkpolicy',
        'allow-dns-egress:networkpolicy',
        'allow-api-egress:networkpolicy',
        'allow-signalr-service-egress:networkpolicy',
        'allow-nginx-gateway-egress:networkpolicy',
    ],
    new_name='3b-network-policies',
    labels=['infra'],
)

# 4-5. Infrastructure (databases & messaging — no deps on app)
k8s_resource(
    'mssql',
    new_name='4-mssql',
    labels=['infra'],
    port_forwards=[
        port_forward(1433, 1433, name='MSSQL'),
    ],
    objects=['mssql-data:persistentvolumeclaim'],
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

k8s_resource(
    'redis',
    new_name='5b-redis',
    labels=['infra'],
    port_forwards=[
        port_forward(6379, 6379, name='Redis'),
    ],
    objects=[
        'redis-config:configmap',
        'redis-data:persistentvolumeclaim',
    ],
)

k8s_resource(
    'mock-oauth2',
    new_name='4b-mock-oauth2',
    labels=['infra'],
    port_forwards=[
        port_forward(9090, 8080, name='OAuth2 Server'),
    ],
    links=[
        link('http://localhost:9090/default/.well-known/openid-configuration', 'OIDC Discovery'),
        link('http://localhost:9090/default/debugger', 'Token Debugger'),
    ],
)

# 6. Application (depends on infra)
k8s_resource(
    'clinic-api',
    new_name='6-clinic-api',
    labels=['app'],
    resource_deps=['4-mssql', '5-rabbitmq', '5b-redis'],
    links=[
        link('https://localhost:8443/api/patients', 'API: Patients (via Gateway)'),
        link('https://localhost:8443/health', 'Health Check (via Gateway)'),
    ],
)

k8s_resource(
    'signalr-service',
    new_name='6b-signalr-service',
    labels=['app'],
    resource_deps=['5-rabbitmq', '5b-redis', '4b-mock-oauth2'],
)

k8s_resource(
    'web-app',
    new_name='6c-web-app',
    labels=['app'],
    # No resource_deps — Next.js dev server starts independently and proxies when ready
)

# 7. Gateway (depends on all app services)
k8s_resource(
    'nginx-gateway',
    new_name='7-nginx-gateway',
    labels=['gateway'],
    port_forwards=[
        port_forward(8443, 8443, name='HTTPS'),
        port_forward(8080, 8080, name='HTTP'),
    ],
    resource_deps=['6-clinic-api', '6b-signalr-service'],
    objects=['nginx-config:configmap'],
    links=[
        link('https://localhost:8443', 'Clinic Dashboard'),
        link('https://localhost:8443/api/scalar/v1', 'API Docs (Scalar)'),
        link('https://localhost:8443/api/patients', 'API: Patients'),
        link('https://localhost:8443/rabbitmq/', 'RabbitMQ Management'),
    ],
)

# 8. Database Migration (auto-runs after MSSQL is ready in dev)
k8s_resource(
    'db-migrate-manual',
    new_name='8-db-migrate',
    labels=['tooling'],
    resource_deps=['4-mssql'],
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
