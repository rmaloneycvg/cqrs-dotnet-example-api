# Multi-stage Dockerfile for the React (Next.js) web app
# Stage 1: Dependencies
FROM node:22-alpine AS deps
WORKDIR /app

COPY web/package.json web/package-lock.json ./
RUN npm ci

# Stage 2: Build
FROM node:22-alpine AS build
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY web/ .

ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PUBLIC_API_URL=http://clinic-api-service:5000
ENV NEXT_PUBLIC_SIGNALR_URL=http://signalr-service:5001/realtime-hub

RUN npm run build

# Stage 3: Production
FROM node:22-alpine AS runtime
WORKDIR /app

RUN addgroup --system --gid 10001 appgroup && \
    adduser --system --uid 10001 -G appgroup appuser

COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

USER 10001
ENV NODE_ENV=production
ENV PORT=3000
EXPOSE 3000

CMD ["node", "server.js"]

# Stage 4: Development
FROM node:22-alpine AS development
WORKDIR /app

COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ .

RUN addgroup --system --gid 10001 appgroup && \
    adduser --system --uid 10001 -G appgroup appuser
RUN chown -R 10001:10001 /app
USER 10001

ENV NODE_ENV=development
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
EXPOSE 3000

CMD ["npm", "run", "dev"]
