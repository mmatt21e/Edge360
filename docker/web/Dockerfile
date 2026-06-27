# syntax=docker/dockerfile:1
# Builds the Blazor WebAssembly app and serves the static output with nginx.
# Build context must be the repository root:
#   docker build -f docker/web/Dockerfile -t edge360-web .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Edge360.sln Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/web/Edge360.Web/Edge360.Web.csproj -c Release -o /app/publish

FROM nginx:1.27-alpine AS runtime
COPY docker/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY docker/web/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Public URL of the API the browser should call (overridable at runtime).
ENV API_BASE_URL=http://localhost:8080
EXPOSE 80
ENTRYPOINT ["/entrypoint.sh"]
