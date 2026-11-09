// ============================================================================
// Dev environment: AKS + ACR pull role + Key Vault + PostgreSQL + 5 databases
// Deploy: az deployment group create (resource group scope)
// ============================================================================

@description('Environment naam: dev ya prod')
param env string = 'dev'

@description('Azure region. Default: resource group ki location')
param location string = resourceGroup().location

@description('ACR naam (Step 17 wala). Sirf lowercase letters aur numbers.')
param acrName string = 'sanghecomacr'

@description('PostgreSQL admin username')
param postgresAdminLogin string = 'pgadmin'

@secure()
@description('PostgreSQL admin password. Command line par pass karein, file mein nahi.')
param postgresAdminPassword string

// Naam unique rakhne ke liye resource group par based suffix
var suffix = uniqueString(resourceGroup().id)
var aksName = 'aks-sanghecom-${env}'
var pgName = 'pg-sanghecom-${env}-${take(suffix, 6)}'
var kvName = 'kv-sg-${env}-${take(suffix, 8)}'

// Har service ka apna database (ADR-001: database per service)
var serviceDatabases = [
  'catalog_db'
  'inventory_db'
  'payment_db'
  'notification_db'
  'orders_db'
]

// AcrPull built-in role ID (Azure ka fixed ID)
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

// ----------------------------------------------------------------------------
// ACR: Step 17 mein bana hua, yahan sirf reference (existing)
// ----------------------------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// ----------------------------------------------------------------------------
// AKS cluster
//  - SystemAssigned identity: cluster ka apna identity
//  - oidcIssuer + workloadIdentity: pods ko bina password Azure identity (Step 20)
//  - 2 nodes Standard_D2s_v5: dev ke liye chhota
// ----------------------------------------------------------------------------
resource aks 'Microsoft.ContainerService/managedClusters@2024-05-01' = {
  name: aksName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: aksName
    oidcIssuerProfile: {
      enabled: true
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
    agentPoolProfiles: [
      {
        name: 'system'
        count: 2
        vmSize: 'Standard_D2s_v5'
        mode: 'System'
        osType: 'Linux'
        osSKU: 'Ubuntu'
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      loadBalancerSku: 'standard'
    }
  }
}

// AKS ke kubelet identity ko ACR se image pull ki permission
resource aksAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, aks.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

// ----------------------------------------------------------------------------
// Key Vault (RBAC mode: access role se, access policy se nahi)
// ----------------------------------------------------------------------------
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// ----------------------------------------------------------------------------
// PostgreSQL Flexible Server (Burstable, dev ke liye sasta)
// ----------------------------------------------------------------------------
resource pg 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: pgName
  location: location
  sku: {
    name: 'Standard_D2ds_v4'
    tier: 'GeneralPurpose'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Dev: Azure ke andar ki services connect kar saken.
// Prod mein VNet + private endpoint hoga, public access nahi.
resource pgFirewallAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: pg
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Har service ka database
resource pgDatabases 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = [for db in serviceDatabases: {
  parent: pg
  name: db
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}]

// Postgres admin password Key Vault mein (Step 20 mein pods yahan se lenge)
resource pgPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'postgres-admin-password'
  properties: {
    value: postgresAdminPassword
  }
}

// ----------------------------------------------------------------------------
// Outputs: deploy ke baad ye values chahiye.
// ----------------------------------------------------------------------------
output aksName string = aks.name
output postgresServerName string = pg.name
output postgresFqdn string = pg.properties.fullyQualifiedDomainName
output keyVaultName string = kv.name
output keyVaultUri string = kv.properties.vaultUri
