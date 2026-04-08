# Build command (from Client root):
#   docker build -f apps/web/Dockerfile -t wordki-web .

FROM node:22-alpine AS build
WORKDIR /app

# Monorepo (npm workspaces): potrzebujemy root package-lock.json oraz workspace packages.
COPY package*.json ./
COPY apps ./apps
COPY packages ./packages

RUN npm ci
RUN npm run build -w web

FROM nginx:1.29-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/apps/web/dist ./

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
