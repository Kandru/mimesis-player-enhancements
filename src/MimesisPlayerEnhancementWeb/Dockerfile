FROM node:22-alpine

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build

# Copy Vite output to mounted volume (see Makefile webinterface target → dist/webinterface/).
CMD ["sh", "-c", "cp -r dist/. /out/"]
