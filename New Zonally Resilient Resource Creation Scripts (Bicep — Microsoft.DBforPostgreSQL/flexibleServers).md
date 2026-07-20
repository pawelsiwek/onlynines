# Create Zonally Resilient Resource — Bicep

**Summary:** Bicep template: create zone-redundant PostgreSQL Flexible Server 'onlynines-pg-01' (primary Zone 2, standby Zone 3, eastus2).

```bicep
// Create zone-redundant Azure Database for PostgreSQL Flexible Server 'onlynines-pg-01'
// Primary Zone: 2 | Standby Zone: 3 | Location: eastus2
// Deploy: az deployment group create --resource-group rg-onlynines-prod --template-file psql.bicep

@description('Name of the PostgreSQL Flexible Server.')
param serverName string = 'onlynines-pg-01'

@description('Azure region.')
param location string = 'eastus2'

@description('Primary availability zone.')
@allowed(['1', '2', '3'])
param zone string = '2'

@description('Standby availability zone (must differ from primary).')
@allowed(['1', '2', '3'])
param standbyZone string = '3'

@description('Admin username.')
param adminUsername string = 'pgadmin'

@description('Admin password.')
@secure()
param adminPassword string = '<PLACEHOLDER>'

@description('SKU tier.')
@allowed(['Burstable', 'GeneralPurpose', 'MemoryOptimized'])
param skuTier string = 'GeneralPurpose'

@description('SKU name.')
param skuName string = 'Standard_D2ds_v5'

@description('Storage size in GB.')
param storageSizeGB int = 32

@description('PostgreSQL major version.')
@allowed(['14', '15', '16'])
param postgresVersion string = '16'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: serverName
  location: location
  sku: { name: skuName, tier: skuTier }
  properties: {
    administratorLogin:         adminUsername
    administratorLoginPassword: adminPassword
    version:                    postgresVersion
    availabilityZone:           zone
    highAvailability: { mode: 'ZoneRedundant', standbyAvailabilityZone: standbyZone }
    storage:  { storageSizeGB: storageSizeGB }
    backup:   { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    network:  { delegatedSubnetResourceId: null }
  }
}

output serverFqdn string = server.properties.fullyQualifiedDomainName

```
