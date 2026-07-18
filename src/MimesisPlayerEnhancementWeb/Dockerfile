# syntax=docker/dockerfile:1
FROM node:22-alpine

WORKDIR /app

COPY package.json package-lock.json* ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci

COPY . .
RUN npm run build

# Copy Vite output to mounted volume (standalone image builds only).
CMD ["sh", "-c", "cp -r dist/. /out/"]
