ARG DECODER_IMAGE
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PlantCare.PhotoProcessing/ PlantCare.PhotoProcessing/
COPY tests/photo-format-fixtures/ tests/photo-format-fixtures/
RUN dotnet publish tests/photo-format-fixtures/PhotoFormatFixtures.csproj -c Release -o /fixtures

FROM ${DECODER_IMAGE} AS fixture
COPY --from=build /fixtures /fixtures
LABEL org.plantcare.test-only="format-fixtures"
ENTRYPOINT ["dotnet", "/fixtures/PhotoFormatFixtures.dll"]
