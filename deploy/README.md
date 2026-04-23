# OnlineGame Deploy Baseline (Fase 1)

Este diretório contém a fundação de deploy local (Docker Compose) e cluster (Kubernetes) para o `auth-service`.

## 1) Docker Compose (local)

Pré-requisitos:
- Docker Desktop

Subir stack:

```bash
cd deploy
docker compose up -d --build
```

Serviços:
- `auth-service`: `http://localhost:8080`
- `auth-db` (PostgreSQL): `localhost:5432`
- `redis`: `localhost:6379`

Parar stack:

```bash
docker compose down
```

## 2) Kubernetes (baseline)

Pré-requisitos:
- Cluster Kubernetes
- `kubectl`
- (opcional) `kustomize`

Aplicar manifests:

```bash
kubectl apply -k deploy/k8s/auth-service
```

Verificar recursos:

```bash
kubectl get all -n onlinegame
```

## 3) Observações da pesquisa (Fase 1)

- Configuração usa segredos e conexões locais em texto claro por design (baseline Unprotected).
- A imagem do `auth-service` está definida como `onlinegame/auth-service:baseline` no manifesto Kubernetes.
- Atualize `deploy/k8s/auth-service/secret.yaml` antes de ambientes compartilhados.