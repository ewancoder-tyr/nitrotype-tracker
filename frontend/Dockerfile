FROM node:latest AS build
ENV PNPM_HOME=/pnpm
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
WORKDIR /usr/src/app

COPY package*.json ./

RUN pnpm install -g @angular/cli
RUN pnpm install

COPY . .
#COPY src/config.production.ts src/config.ts
RUN pnpm run build

FROM nginxinc/nginx-unprivileged:stable-alpine
COPY ./nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /usr/src/app/dist/nitrotype-tracker/browser /usr/share/nginx/html

CMD ["nginx", "-g", "daemon off;"]
