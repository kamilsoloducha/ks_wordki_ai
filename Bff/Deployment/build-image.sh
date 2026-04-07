#!/usr/bin/env bash
set -euo pipefail

DEPLOYMENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BFF_ROOT="$(cd "${DEPLOYMENT_DIR}/.." && pwd)"
ENV_FILE="${DEPLOYMENT_DIR}/deployment.env"

if [[ -f "${ENV_FILE}" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "${ENV_FILE}"
  set +a
else
  echo "Brak ${ENV_FILE} — użyj domyślnych nazw obrazu lub skopiuj env.example:" >&2
  echo "  cp ${DEPLOYMENT_DIR}/env.example ${ENV_FILE}" >&2
fi

IMAGE_NAME="${IMAGE_NAME:-wordki-bff}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

if [[ -n "${1:-}" ]]; then
  IMAGE_TAG="$1"
fi

cd "${BFF_ROOT}"
echo "Building ${IMAGE_NAME}:${IMAGE_TAG} (context: ${BFF_ROOT})"
docker build -t "${IMAGE_NAME}:${IMAGE_TAG}" -f Dockerfile .

echo "Gotowe: ${IMAGE_NAME}:${IMAGE_TAG}"
