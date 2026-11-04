FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PlantCare.PhotoTransport/ PlantCare.PhotoTransport/
COPY PlantCare.PhotoTransport.Tests/ PlantCare.PhotoTransport.Tests/
COPY tests/photo-transport-client/ tests/photo-transport-client/
RUN dotnet test PlantCare.PhotoTransport.Tests/PlantCare.PhotoTransport.Tests.csproj -c Release --verbosity quiet --blame-hang-timeout 30s
RUN dotnet publish tests/photo-transport-client/PhotoTransportClient.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS client
WORKDIR /app
COPY --from=build /out/ ./
ENV DOTNET_EnableDiagnostics=0
USER app
ENTRYPOINT ["dotnet", "PhotoTransportClient.dll"]

# Local acceptance launcher, not a deployment artifact. Only this trusted test
# gateway receives the daemon socket; the API/client and decoders never do.
FROM docker:cli AS docker-cli
FROM python:3.13-slim AS gateway
COPY --from=docker-cli /usr/local/bin/docker /usr/bin/docker
COPY scripts/photo_gateway.py /app/photo_gateway.py
RUN mkdir -p /run/plantcare-photo && chown 0:1654 /run/plantcare-photo && chmod 750 /run/plantcare-photo
ENTRYPOINT ["python3", "/app/photo_gateway.py"]

FROM gateway AS gateway-tests
WORKDIR /src
COPY scripts/photo_gateway.py scripts/photo_gateway.py
COPY scripts/photo_bundle.py scripts/photo_bundle.py
COPY scripts/photo_capacity.py scripts/photo_capacity.py
COPY tests/test_photo_gateway.py tests/test_photo_gateway.py
COPY tests/test_photo_bundle.py tests/test_photo_bundle.py
COPY tests/test_photo_capacity.py tests/test_photo_capacity.py
COPY deploy/plantcare-photo-gateway.service.example deploy/compose.photo-processing.yaml.example deploy/
RUN python3 -m unittest discover -s tests -p 'test_photo_*.py'
