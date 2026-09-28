FROM node:24-alpine AS build
WORKDIR /portal
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /portal/dist/portal/browser /usr/share/nginx/html
EXPOSE 80
