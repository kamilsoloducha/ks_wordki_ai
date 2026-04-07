#!/usr/bin/env bash
set -euo pipefail

DEPLOYMENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${DEPLOYMENT_DIR}/deployment.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Brak pliku ${ENV_FILE}" >&2
  echo "Utwórz go z szablonu: cp ${DEPLOYMENT_DIR}/env.example ${ENV_FILE}" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "${ENV_FILE}"
set +a

IMAGE_NAME="${IMAGE_NAME:-wordki-bff}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
CONTAINER_NAME="${CONTAINER_NAME:-wordki-bff}"
HOST_PORT="${HOST_PORT:-5000}"

IMAGE_REF="${IMAGE_NAME}:${IMAGE_TAG}"

if ! docker image inspect "${IMAGE_REF}" >/dev/null 2>&1; then
  echo "Obraz ${IMAGE_REF} nie istnieje lokalnie. Buduję..." >&2
  "${DEPLOYMENT_DIR}/build-image.sh"
fi

if docker ps -a --format '{{.Names}}' | grep -qx "${CONTAINER_NAME}"; then
  echo "Zatrzymywanie i usuwanie kontenera ${CONTAINER_NAME}..."
  docker stop "${CONTAINER_NAME}" >/dev/null
  docker rm "${CONTAINER_NAME}" >/dev/null
fi

echo "Uruchamianie ${CONTAINER_NAME} z obrazu ${IMAGE_REF}, sieć host, aplikacja na porcie ${HOST_PORT} (ASPNETCORE_URLS)..."

# Plik tylko dla dockera: bez zmiennych skryptowych, żeby nie duplikować niepotrzebnych wpisów.
RUNTIME_ENV="${DEPLOYMENT_DIR}/.runtime.env.tmp"
trap 'rm -f "${RUNTIME_ENV}"' EXIT

grep -v '^[[:space:]]*#' "${ENV_FILE}" | grep -v '^[[:space:]]*$' | grep -v '^IMAGE_NAME=' | grep -v '^IMAGE_TAG=' | grep -v '^CONTAINER_NAME=' | grep -v '^HOST_PORT=' > "${RUNTIME_ENV}"

docker run -d \
  --name "${CONTAINER_NAME}" \
  --network host \
  --restart unless-stopped \
  --env-file "${RUNTIME_ENV}" \
  "${IMAGE_REF}"

echo "Kontener ${CONTAINER_NAME} działa. API: http://localhost:${HOST_PORT}/"
docker ps --filter "name=^${CONTAINER_NAME}$"
