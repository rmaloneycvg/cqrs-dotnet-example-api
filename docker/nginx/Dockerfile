FROM nginx:1.27-alpine

# Generate self-signed TLS certificate for local development
RUN apk add --no-cache openssl && \
    mkdir -p /etc/nginx/ssl && \
    openssl req -x509 -nodes -days 365 \
      -newkey rsa:2048 \
      -keyout /etc/nginx/ssl/tls.key \
      -out /etc/nginx/ssl/tls.crt \
      -subj "/CN=localhost" \
      -addext "subjectAltName=DNS:localhost,DNS:*.localhost,IP:127.0.0.1" && \
    apk del openssl

# Create temp dirs for non-root operation
RUN mkdir -p /tmp/client_body /tmp/proxy /tmp/fastcgi /tmp/uwsgi /tmp/scgi && \
    mkdir -p /var/cache/nginx/api_cache && \
    chown -R nginx:nginx /tmp /var/cache/nginx /var/log/nginx /etc/nginx/ssl

EXPOSE 8080 8443

# Security: run as non-root (matches k8s Pod Security Admission restricted profile)
USER nginx
