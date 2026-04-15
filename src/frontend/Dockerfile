FROM node:18-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Set API URLs for Docker environment
ENV VITE_SECURITY_API_URL=http://localhost:5001
ENV VITE_PROFILE_API_URL=http://localhost:5002
ENV VITE_WORK_API_URL=http://localhost:5003
ENV VITE_UTILITY_API_URL=http://localhost:5200
ENV VITE_BILLING_API_URL=http://localhost:5300
ENV VITE_APP_NAME="Nexus 2.0"

RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1
CMD ["nginx", "-g", "daemon off;"]
