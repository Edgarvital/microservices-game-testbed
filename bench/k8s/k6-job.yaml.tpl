# Job de carga REST (k6) contra o matchmaking-service, executado DENTRO do mesh.
# Placeholders substituidos por envsubst no run.sh.
apiVersion: batch/v1
kind: Job
metadata:
  name: bench-k6
  namespace: ${NAMESPACE}
  labels:
    app: bench-k6
spec:
  backoffLimit: 0
  ttlSecondsAfterFinished: 600
  template:
    metadata:
      labels:
        app: bench-k6
      annotations:
        ${K6_SIDECAR_ANNOTATION}
    spec:
      restartPolicy: Never
      volumes:
        - name: script
          configMap:
            name: bench-k6-script
      containers:
        - name: k6
          image: ${K6_IMAGE}
          env:
            - name: BASE_URL
              value: "http://matchmaking-service:8080"
            - name: PLAYER_ID
              value: "${PLAYER_ID}"
          args:
            - "run"
            - "--vus=${K6_VUS}"
            - "--duration=${K6_DURATION}"
            - "/scripts/rest.js"
          volumeMounts:
            - name: script
              mountPath: /scripts
