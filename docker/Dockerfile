# syntax=docker/dockerfile:1
# Multi-stage build for the Edge360 API. Build context must be the repository root:
#   docker build -f docker/Dockerfile -t edge360-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching) — copy project files and shared build props.
COPY Edge360.sln Directory.Build.props ./
COPY src/server/Edge360.Domain/Edge360.Domain.csproj src/server/Edge360.Domain/
COPY src/server/Edge360.Application/Edge360.Application.csproj src/server/Edge360.Application/
COPY src/server/Edge360.Infrastructure/Edge360.Infrastructure.csproj src/server/Edge360.Infrastructure/
COPY src/server/Edge360.Api/Edge360.Api.csproj src/server/Edge360.Api/
RUN dotnet restore src/server/Edge360.Api/Edge360.Api.csproj

# Copy the rest and publish.
COPY src/ src/
RUN dotnet publish src/server/Edge360.Api/Edge360.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Run as a non-root user.
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Edge360.Api.dll"]
