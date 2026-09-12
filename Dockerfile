FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY backend/ ./
WORKDIR /src/src/AgriCheck.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
CMD ["sh", "-c", "dotnet AgriCheck.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
