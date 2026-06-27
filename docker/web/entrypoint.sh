#!/bin/sh
# Injects the runtime API base URL into the published static config, then starts nginx.
set -e
: "${API_BASE_URL:=http://localhost:8080}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{
  "ApiBaseUrl": "${API_BASE_URL}"
}
EOF

exec nginx -g 'daemon off;'
