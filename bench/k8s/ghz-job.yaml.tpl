# Job de carga gRPC (ghz) contra o inventory-service, executado DENTRO do mesh
# para que o sidecar aplique (ou nao) o mTLS conforme o cenario.
# Placeholders substituidos por envsubst no run.sh.
apiVersion: batch/v1
kind: Job
metadata:
  name: bench-ghz
  namespace: ${NAMESPACE}
  labels:
    app: bench-ghz
spec:
  backoffLimit: 0
  ttlSecondsAfterFinished: 600
  template:
    metadata:
      labels:
        app: bench-ghz
      annotations:
        # A carga precisa passar pelo sidecar; sem ele, STRICT rejeitaria.
        # ${GHZ_SIDECAR_ANNOTATION} vira 'sidecar.istio.io/inject: "true"' (B/C)
        # ou '"false"' (cenario A, sem mesh).
        ${GHZ_SIDECAR_ANNOTATION}
    spec:
      restartPolicy: Never
      volumes:
        - name: proto
          configMap:
            name: bench-proto
      containers:
        - name: ghz
          image: ${GHZ_IMAGE}
          args:
            - "--insecure"
            - "--proto=/proto/player_inventory.proto"
            - "--call=onlinegame.inventory.v1.InventoryService.GetBattleStats"
            - "-d"
            - '{"player_id":"${PLAYER_ID}"}'
            - "-n"
            - "${GHZ_N}"
            - "-c"
            - "${GHZ_C}"
            - "--connections=${GHZ_CONN}"
            - "--skipFirst=${GHZ_SKIP}"
            - "--format=json"
            - "inventory-service:8080"
          volumeMounts:
            - name: proto
              mountPath: /proto
