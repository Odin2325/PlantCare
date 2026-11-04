# Test-only wrapper around the accepted decoder. The short delay lets the test
# freeze a genuinely unfinished job before output exists, rather than racing a
# tiny image that may already have completed. Never package/deploy this image.
ARG DECODER_IMAGE
FROM ${DECODER_IMAGE}
LABEL org.plantcare.test-only="interruption-delay"
ENTRYPOINT ["/bin/sh", "-c", "sleep 5; exec dotnet PlantCare.PhotoProcessing.dll --normalize \"$@\"", "photo-interruption-delay"]
