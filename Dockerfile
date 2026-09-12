FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY backend/ ./
WORKDIR /src/src/AgriCheck.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-liberation fonts-dejavu-core \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV UPLOADS_ROOT=/app/uploads
EXPOSE 8080
CMD ["sh", "-c", "dotnet AgriCheck.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
