# syntax=docker/dockerfile:1

FROM node:24-bookworm-slim AS frontend
WORKDIR /src/plant-care-web
RUN corepack enable
COPY plant-care-web/package.json plant-care-web/pnpm-lock.yaml plant-care-web/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile
COPY plant-care-web/ ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PlantCare.Api/PlantCare.Api.csproj PlantCare.Api/
COPY PlantCare.Application/PlantCare.Application.csproj PlantCare.Application/
COPY PlantCare.Domain/PlantCare.Domain.csproj PlantCare.Domain/
COPY PlantCare.Infrastructure/PlantCare.Infrastructure.csproj PlantCare.Infrastructure/
COPY PlantCare.Worker/PlantCare.Worker.csproj PlantCare.Worker/
RUN dotnet restore PlantCare.Api/PlantCare.Api.csproj \
    && dotnet restore PlantCare.Worker/PlantCare.Worker.csproj
COPY . ./
COPY --from=frontend /src/plant-care-web/dist/plant-care-web/browser/ PlantCare.Api/wwwroot/
RUN dotnet publish PlantCare.Api/PlantCare.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/api
RUN dotnet publish PlantCare.Worker/PlantCare.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/api ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlantCare.Api.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
WORKDIR /app
COPY --from=build /app/worker ./
USER app
ENTRYPOINT ["dotnet", "PlantCare.Worker.dll"]
