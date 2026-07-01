# Runbook de execução (cluster cloud gerenciado)

Passo a passo para produzir os dados do artigo num cluster **cloud gerenciado** (EKS/GKE/AKS). Fluxo pensado para execução conduzida: você provisiona o cluster e o registry e disponibiliza o acesso; a partir daí os scripts fazem o resto.

## Visão geral

```
provisionar cluster + registry  ->  kubeconfig ativo  ->  build/push imagens
   ->  bench/setup.sh (Istio + deploy + seed)  ->  bench/run.sh  ->  summary.md
```

## 0. O que preciso de você para conduzir

1. **Cluster cloud provisionado** (ver seção 1) e **`kubeconfig` ativo na máquina desta sessão** (para o meu `kubectl` acessar o cluster). Confirme com:
   ```bash
   kubectl config current-context && kubectl get nodes
   ```
2. **Um container registry** acessível pelo cluster e o login feito no `docker` local (ver seção 2).
3. Confirmar as specs dos nós (para registrar no `environment.txt` e garantir validade): recomendado **nós dedicados**, sem outra carga, tamanho fixo entre cenários.

> Segurança: use credenciais de escopo restrito e temporárias para este trabalho; revogue depois. Não cole segredos no chat: configure o kubeconfig/registry localmente e eu uso o contexto ativo.

## 1. Provisionar o cluster

Escolha o provedor. Recomendação: 2-3 nós dedicados (ex.: 4 vCPU / 16 GB), mesma zona, para reduzir ruído de rede.

- **GKE:**
  ```bash
  gcloud container clusters create bench --num-nodes=3 --machine-type=e2-standard-4 --zone=<zona>
  gcloud container clusters get-credentials bench --zone=<zona>
  ```
- **EKS:**
  ```bash
  eksctl create cluster --name bench --nodes 3 --node-type m5.xlarge --region <regiao>
  # kubeconfig e configurado pelo eksctl automaticamente
  ```
- **AKS:**
  ```bash
  az aks create -g <rg> -n bench --node-count 3 --node-vm-size Standard_D4s_v3 --generate-ssh-keys
  az aks get-credentials -g <rg> -n bench
  ```

Instale o **metrics-server** se o provedor não o incluir (GKE/AKS já incluem; no EKS pode ser preciso instalar) para o `kubectl top`.

## 2. Registry e imagens

Faça login no registry e publique as imagens:

```bash
# exemplos de login
#   ECR:  aws ecr get-login-password --region <r> | docker login --username AWS --password-stdin <acct>.dkr.ecr.<r>.amazonaws.com
#   GCR/Artifact: gcloud auth configure-docker <host>
#   Docker Hub:   docker login

REGISTRY=<seu-registry> TAG=baseline bash bench/build-images.sh
```

No ECR, crie os repositórios (`onlinegame/auth-service`, etc.) antes do push, ou habilite criação automática.

## 3. Setup do cluster (Istio + serviços + seed)

```bash
REGISTRY=<seu-registry> TAG=baseline bash bench/setup.sh
```

O `setup.sh` instala o Istio (com native sidecars, para os Jobs de carga completarem), implanta os quatro serviços apontando para o seu registry, aguarda o rollout e semeia o inventário de teste. Ao final, confirme que os pods estão `Running`.

## 4. Executar o benchmark

```bash
# padrao: cenarios A B C, 5 repeticoes
bash bench/run.sh

# execucao mais robusta para o artigo (mais requests e repeticoes)
REPS=10 GHZ_N=50000 GHZ_C=100 K6_VUS=100 K6_DURATION=120s bash bench/run.sh
```

Saída em `bench/results/<stamp>/`: dados brutos por cenário/repetição + `summary.md`/`summary.csv` (média ± desvio, overhead B→C e A→C).

## 5. Consolidar para o artigo

- Cole `summary.md` em `bench/results/RESULTS_TEMPLATE.md` (e preencha o quadro de ambiente).
- Versione os `results/<stamp>/` da rodada final: `git add -f bench/results/<stamp>`.
- Repita em dias/horários diferentes se quiser robustez estatística.

## 6. Limpeza

```bash
# destrua o cluster para nao gerar custo
gcloud container clusters delete bench --zone=<zona>   # ou eksctl/az equivalente
```

## Checklist rápido

- [ ] Cluster up, `kubectl get nodes` OK, metrics-server presente
- [ ] Registry logado, `build-images.sh` publicou as 4 imagens
- [ ] `setup.sh` concluiu, pods `Running` (2 containers = app + sidecar nos cenários B/C)
- [ ] Inventário semeado (gRPC não retorna NOT_FOUND)
- [ ] `run.sh` gerou `summary.md`
- [ ] Ambiente documentado em `environment.txt`
- [ ] Cluster destruído após coletar os dados
</content>
