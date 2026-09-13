FROM mcr.microsoft.com/dotnet/sdk:7.0-bookworm-slim AS build
WORKDIR /src
COPY backend/ ./
WORKDIR /src/src/AgriCheck.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-bookworm-slim AS runtime
WORKDIR /app
RUN apt-get -o Acquire::Check-Valid-Until=false update \
    && apt-get -o Acquire::Check-Valid-Until=false install -y --no-install-recommends \
        fontconfig \
        fonts-liberation \
        fonts-dejavu-core \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY backend/deploy/certificate-templates /app/seed-certificate-templates
ENV UPLOADS_ROOT=/app/uploads
EXPOSE 8080
CMD ["sh", "-c", "dotnet AgriCheck.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
