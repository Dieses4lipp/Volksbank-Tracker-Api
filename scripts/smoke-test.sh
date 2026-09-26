#!/bin/sh
# Run the image exactly as production does (read-only root, no caps, data volume)
# and probe it. Used by CI before pushing; runnable locally:
#   docker build -t volksbanktracker:ci . && scripts/smoke-test.sh volksbanktracker:ci
# Never touches the bank: no FinTS credentials are configured.
set -eu
IMAGE=${1:?usage: smoke-test.sh <image>}
KEY=ci-smoke-key
URL=http://127.0.0.1:8080

cleanup() {
  status=$?
  [ "$status" -ne 0 ] && docker logs smoke || true
  docker rm -f smoke >/dev/null 2>&1 || true
  docker volume rm smoke-data >/dev/null 2>&1 || true
  exit "$status"
}
trap cleanup EXIT

docker run -d --name smoke -p 8080:8080 \
  -e Api__Key="$KEY" \
  -v smoke-data:/data \
  --read-only --tmpfs /tmp --cap-drop ALL --security-opt no-new-privileges \
  "$IMAGE" >/dev/null

code() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

for i in $(seq 1 30); do [ "$(code "$URL/health")" = 200 ] && break; sleep 1; done

test "$(code "$URL/health")"                                  = 200  # migrations ran, DB reachable
test "$(code "$URL/api/categories")"                          = 401  # key enforced
test "$(code -H "X-Api-Key: wrong" "$URL/api/categories")"    = 401
test "$(code -H "X-Api-Key: $KEY" "$URL/api/categories")"     = 200  # seeded DB readable
test "$(code -H "X-Api-Key: $KEY" "$URL/swagger/index.html")" = 404  # Swagger stays Development-only
echo "smoke test passed"
