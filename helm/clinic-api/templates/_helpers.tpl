{{/*
Common labels applied to all resources
*/}}
{{- define "clinic-api.labels" -}}
app.kubernetes.io/name: {{ .Chart.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end }}

{{/*
Selector labels for clinic-api
*/}}
{{- define "clinic-api.selectorLabels" -}}
app: clinic-api
{{- end }}

{{/*
Selector labels for nginx-gateway
*/}}
{{- define "nginxGateway.selectorLabels" -}}
app: nginx-gateway
{{- end }}

{{/*
Selector labels for mssql
*/}}
{{- define "mssql.selectorLabels" -}}
app: mssql
{{- end }}

{{/*
Selector labels for rabbitmq
*/}}
{{- define "rabbitmq.selectorLabels" -}}
app: rabbitmq
{{- end }}

{{/*
Namespace
*/}}
{{- define "clinic-api.namespace" -}}
{{ .Values.global.namespace | default "clinic-api" }}
{{- end }}
