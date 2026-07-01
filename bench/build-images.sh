#!/usr/bin/env bash
#
# Builda e publica as imagens dos servicos num registry acessivel pelo cluster
# cloud (ECR/GCR/ACR/Docker Hub). Necessario porque os manifests usam
# imagePullPolicy: IfNotPresent com imagens locais, que nao existem no cluster.
#
# Uso:
#   REGISTRY=<seu-registry> [TAG=baseline] bash bench/build-images.sh
# Ex.:
#   REGISTRY=123456789.dkr.ecr.us-east-1.amazonaws.com bash bench/build-images.sh
#   REGISTRY=docker.io/edgarvital TAG=baseline bash bench/build-images.sh
#
# Faca login no registry antes (docker login / aws ecr get-login-password | docker login ...).
set -euo pipefail

REGISTRY=${REGISTRY:?defina REGISTRY (ex.: docker.io/seu-usuario ou <acct>.dkr.ecr.<regiao>.amazonaws.com)}
TAG=${TAG:-baseline}
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

build_push() {
  local name="$1" context="$2" dockerfile="$3"
  local image="$REGISTRY/onlinegame/$name:$TAG"
  echo "==> build $image (context=$context)"
  docker build -t "$image" -f "$dockerfile" "$context"
  echo "==> push $image"
  docker push "$image"
}

# Contextos conforme deploy/docker-compose.yml
build_push auth-service        "services/auth-service"        "services/auth-service/Dockerfile"
build_push inventory-service   "."                            "services/inventory-service/Dockerfile"   # precisa da raiz p/ o proto
build_push matchmaking-service "services/matchmaking-service" "services/matchmaking-service/Dockerfile"
build_push battle-service      "services/battle-service"      "services/battle-service/Dockerfile"

echo ""
echo "Imagens publicadas em $REGISTRY/onlinegame/*:$TAG"
echo "Use o mesmo REGISTRY/TAG em bench/setup.sh para o override das imagens."
