# Tiltfile for CQRS .NET Example API
# Orchestrates: MSSQL, RabbitMQ, ClinicApi with live-reload
# Single source of truth: helm/clinic-api/ chart with values-dev.yaml

# ─── Configuration ───────────────────────────────────────────────────────────

config.define_bool("telemetry")
cfg = config.parse()
enable_telemetry = cfg.get("telemetry", False)

# ─── Helm Chart (single source of k8s manifests) ─────────────────────────────

k8s_yaml(helm(
    './helm/clinic-api',
    name='clinic-api',
    values=['./helm/clinic-api/values-dev.yaml'],
))

# ─── Docker Builds ───────────────────────────────────────────────────────────

# NGINX Gateway
docker_build(
    'nginx-gateway-image',
    context='.',
    dockerfile='docker/nginx/Dockerfile',
    live_update=[
        fall_back_on(['docker/nginx/Dockerfile']),
    ],
)

# Clinic API with live_update for hot reload
docker_build(
    'clinic-api-image',
    context='.',
    dockerfile='docker/api/Dockerfile',
    target='development',
    live_update=[
        sync('./src', '/src/src'),
        sync('./global.json', '/src/global.json'),
        sync('./Directory.Build.props', '/src/Directory.Build.props'),
        fall_back_on(['./src/ClinicApi/ClinicApi.csproj']),
    ],
)

# ─── Resource Configuration ──────────────────────────────────────────────────

# Infrastructure
k8s_resource(
    'mssql',
    labels=['infra'],
    port_forwards=['1433:1433'],
)

k8s_resource(
    'rabbitmq',
    labels=['infra'],
    port_forwards=[
        '5672:5672',   # AMQP
        '15672:15672', # Management UI
    ],
)

# Gateway
k8s_resource(
    'nginx-gateway',
    labels=['gateway'],
    port_forwards=['8443:443', '8080:80'],
    resource_deps=['clinic-api'],
)

# Application
k8s_resource(
    'clinic-api',
    labels=['app'],
    port_forwards=['5000:5000'],
    resource_deps=['mssql', 'rabbitmq'],
)

# Jobs (manual trigger for migrations)
k8s_resource(
    'db-migrate-manual',
    labels=['tooling'],
    resource_deps=['mssql'],
    auto_init=False,
    trigger_mode=TRIGGER_MODE_MANUAL,
)

# ─── Resource Groups ─────────────────────────────────────────────────────────
# Grouped in Tilt UI:
# - gateway: nginx-gateway
# - infra: mssql, rabbitmq
# - app: clinic-api
# - tooling: db-migrate-manual
