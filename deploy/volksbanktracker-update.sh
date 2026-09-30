#!/bin/sh
set -eu
IMAGE=ghcr.io/dieses4lipp/volksbank-tracker-api:main
cd /opt/volksbanktracker

before=$(docker image inspect -f '{{.Id}}' "$IMAGE" 2>/dev/null || echo none)
docker compose pull --quiet
after=$(docker image inspect -f '{{.Id}}' "$IMAGE")

[ "$before" = "$after" ] && exit 0

echo "new image ${after#sha256:} (was ${before#sha256:}) - deploying"
docker compose up -d

status=starting
for i in $(seq 1 12); do
  status=$(docker inspect -f '{{.State.Health.Status}}' volksbanktracker)
  [ "$status" = healthy ] && break
  sleep 5
done
echo "health: $status"
docker image prune -f >/dev/null
[ "$status" = healthy ]
