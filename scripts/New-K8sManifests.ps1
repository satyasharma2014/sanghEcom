# ============================================================================
# scripts/New-K8sManifests.ps1
# k8s/<env>/manifests.yaml banata hai (namespace, identity, secrets mapping,
# configmap, rabbitmq, 5 APIs + migration Jobs, gateway, ingress).
# Image tag yahan IMAGE_TAG hai, CD pipeline ise git SHA se badalta hai.
# ============================================================================
param(
    [Parameter(Mandatory)][ValidateSet('dev', 'prod')][string]$Env,
    [Parameter(Mandatory)][string]$Acr,           # sanghecomacr
    [Parameter(Mandatory)][string]$ClientId,      # workload identity client id
    [Parameter(Mandatory)][string]$KeyVault,      # kv-sg-dev-ss01
    [Parameter(Mandatory)][string]$TenantId,
    [Parameter(Mandatory)][string]$IngressHost    # sanghecom-dev-api.centralindia.cloudapp.azure.com
)
$ErrorActionPreference = 'Stop'
$acrServer = "$Acr.azurecr.io"

function Render([string]$tpl, [hashtable]$map) {
    foreach ($k in $map.Keys) { $tpl = $tpl.Replace("[[$k]]", [string]$map[$k]) }
    return $tpl
}

$namespace = @'
apiVersion: v1
kind: Namespace
metadata:
  name: sanghecom
'@

$identity = @'
apiVersion: v1
kind: ServiceAccount
metadata:
  name: sanghecom-sa
  namespace: sanghecom
  annotations:
    azure.workload.identity/client-id: "[[CLIENTID]]"
---
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: sanghecom-kv
  namespace: sanghecom
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "false"
    clientID: "[[CLIENTID]]"
    keyvaultName: "[[KV]]"
    tenantId: "[[TENANT]]"
    objects: |
      array:
        - |
          objectName: catalog-db-connection
          objectType: secret
        - |
          objectName: inventory-db-connection
          objectType: secret
        - |
          objectName: payment-db-connection
          objectType: secret
        - |
          objectName: notification-db-connection
          objectType: secret
        - |
          objectName: orders-db-connection
          objectType: secret
        - |
          objectName: rabbitmq-username
          objectType: secret
        - |
          objectName: rabbitmq-password
          objectType: secret
  secretObjects:
    - secretName: catalog-db
      type: Opaque
      data:
        - objectName: catalog-db-connection
          key: connection-string
    - secretName: inventory-db
      type: Opaque
      data:
        - objectName: inventory-db-connection
          key: connection-string
    - secretName: payment-db
      type: Opaque
      data:
        - objectName: payment-db-connection
          key: connection-string
    - secretName: notification-db
      type: Opaque
      data:
        - objectName: notification-db-connection
          key: connection-string
    - secretName: orders-db
      type: Opaque
      data:
        - objectName: orders-db-connection
          key: connection-string
    - secretName: rabbitmq-credentials
      type: Opaque
      data:
        - objectName: rabbitmq-username
          key: username
        - objectName: rabbitmq-password
          key: password
'@

$configMap = @'
apiVersion: v1
kind: ConfigMap
metadata:
  name: sanghecom-config
  namespace: sanghecom
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  RabbitMq__Host: "rabbitmq"
  RabbitMq__Port: "5672"
  RabbitMq__Exchange: "sanghecom.events"
  Services__Catalog: "http://catalog-api:8080"
  Services__Inventory: "http://inventory-api:8080"
  Services__Payment: "http://payment-api:8080"
'@

# Pods ka common hissa: workload identity label, service account, CSI volume
$podCommon = @'
      serviceAccountName: sanghecom-sa
      volumes:
        - name: kv-secrets
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: sanghecom-kv
'@

$rabbit = @'
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq
  namespace: sanghecom
  labels:
    app: rabbitmq
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: sanghecom-sa
      containers:
        - name: rabbitmq
          image: rabbitmq:3-management
          ports:
            - containerPort: 5672
          env:
            - name: RABBITMQ_DEFAULT_USER
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-credentials
                  key: username
            - name: RABBITMQ_DEFAULT_PASS
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-credentials
                  key: password
          readinessProbe:
            tcpSocket:
              port: 5672
            initialDelaySeconds: 20
            periodSeconds: 10
          volumeMounts:
            - name: kv-secrets
              mountPath: /mnt/secrets-store
              readOnly: true
      volumes:
        - name: kv-secrets
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: sanghecom-kv
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq
  namespace: sanghecom
spec:
  selector:
    app: rabbitmq
  ports:
    - name: amqp
      port: 5672
      targetPort: 5672
'@

$apiTemplate = @'
apiVersion: apps/v1
kind: Deployment
metadata:
  name: [[NAME]]
  namespace: sanghecom
  labels:
    app: [[NAME]]
spec:
  replicas: 2
  selector:
    matchLabels:
      app: [[NAME]]
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: [[NAME]]
        azure.workload.identity/use: "true"
    spec:
      serviceAccountName: sanghecom-sa
      containers:
        - name: [[NAME]]
          image: [[ACR]]/[[NAME]]:IMAGE_TAG
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: sanghecom-config
          env:
            - name: ConnectionStrings__[[DBKEY]]
              valueFrom:
                secretKeyRef:
                  name: [[DB]]-db
                  key: connection-string
            - name: RabbitMq__UserName
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-credentials
                  key: username
            - name: RabbitMq__Password
              valueFrom:
                secretKeyRef:
                  name: rabbitmq-credentials
                  key: password
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 20
          resources:
            requests:
              cpu: "100m"
              memory: "128Mi"
            limits:
              cpu: "500m"
              memory: "256Mi"
          securityContext:
            runAsNonRoot: true
            allowPrivilegeEscalation: false
          volumeMounts:
            - name: kv-secrets
              mountPath: /mnt/secrets-store
              readOnly: true
      serviceAccountName: sanghecom-sa
      volumes:
        - name: kv-secrets
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: sanghecom-kv
---
apiVersion: v1
kind: Service
metadata:
  name: [[NAME]]
  namespace: sanghecom
spec:
  selector:
    app: [[NAME]]
  ports:
    - port: 8080
      targetPort: 8080
'@

$migrationTemplate = @'
apiVersion: batch/v1
kind: Job
metadata:
  name: [[NAME]]-migrate
  namespace: sanghecom
  labels:
    component: migration
spec:
  backoffLimit: 3
  template:
    metadata:
      labels:
        app: [[NAME]]-migrate
        azure.workload.identity/use: "true"
    spec:
      restartPolicy: Never
      serviceAccountName: sanghecom-sa
      containers:
        - name: migrate
          image: [[ACR]]/[[NAME]]:IMAGE_TAG
          envFrom:
            - configMapRef:
                name: sanghecom-config
          env:
            - name: RUN_MIGRATIONS
              value: "true"
            - name: ConnectionStrings__[[DBKEY]]
              valueFrom:
                secretKeyRef:
                  name: [[DB]]-db
                  key: connection-string
          volumeMounts:
            - name: kv-secrets
              mountPath: /mnt/secrets-store
              readOnly: true
      volumes:
        - name: kv-secrets
          csi:
            driver: secrets-store.csi.k8s.io
            readOnly: true
            volumeAttributes:
              secretProviderClass: sanghecom-kv
'@

$gateway = @'
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: sanghecom
  labels:
    app: api-gateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: api-gateway
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
        - name: api-gateway
          image: [[ACR]]/api-gateway:IMAGE_TAG
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: sanghecom-config
          env:
            - name: ReverseProxy__Clusters__catalog__Destinations__catalog1__Address
              value: "http://catalog-api:8080"
            - name: ReverseProxy__Clusters__orders__Destinations__orders1__Address
              value: "http://orders-api:8080"
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 15
            periodSeconds: 20
          resources:
            requests:
              cpu: "100m"
              memory: "128Mi"
            limits:
              cpu: "500m"
              memory: "256Mi"
          securityContext:
            runAsNonRoot: true
            allowPrivilegeEscalation: false
---
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: sanghecom
spec:
  selector:
    app: api-gateway
  ports:
    - port: 80
      targetPort: 8080
'@

$ingress = @'
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: sanghecom-ingress
  namespace: sanghecom
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
    - hosts:
        - [[HOST]]
      secretName: sanghecom-tls
  rules:
    - host: [[HOST]]
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: api-gateway
                port:
                  number: 80
'@

# Services list: naam, database key (appsettings ke ConnectionStrings key ke saath)
$services = @(
    @{ Name = 'catalog-api';      Db = 'catalog';      DbKey = 'CatalogDb' }
    @{ Name = 'inventory-api';    Db = 'inventory';    DbKey = 'InventoryDb' }
    @{ Name = 'payment-api';      Db = 'payment';      DbKey = 'PaymentDb' }
    @{ Name = 'notification-api'; Db = 'notification'; DbKey = 'NotificationDb' }
    @{ Name = 'orders-api';       Db = 'orders';       DbKey = 'OrdersDb' }
)

$parts = New-Object System.Collections.Generic.List[string]
$parts.Add($namespace)
$parts.Add((Render $identity @{ CLIENTID = $ClientId; KV = $KeyVault; TENANT = $TenantId }))
$parts.Add($configMap)
$parts.Add($rabbit)

foreach ($svc in $services) {
    $map = @{ NAME = $svc.Name; ACR = $acrServer; DB = $svc.Db; DBKEY = $svc.DbKey }
    $parts.Add((Render $apiTemplate $map))
    $parts.Add((Render $migrationTemplate $map))
}

$parts.Add((Render $gateway @{ ACR = $acrServer }))
$parts.Add((Render $ingress @{ HOST = $IngressHost }))

$text = ($parts -join "`n---`n") + "`n"

$outDir = Join-Path $PSScriptRoot "..\k8s\$Env"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outFile = Join-Path $outDir 'manifests.yaml'

# UTF-8 without BOM (kubectl BOM se error de sakta hai)
[System.IO.File]::WriteAllText((Resolve-Path $outDir | Join-Path -ChildPath 'manifests.yaml'), $text, [System.Text.UTF8Encoding]::new($false))

Write-Host "Written: $outFile"
