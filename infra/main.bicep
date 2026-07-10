// OnlyNines — the deliberately-cheap production ($40/mo class).
// This architecture is the conference demo victim. Do not "fix" it here;
// fixing it live IS the talk.

@description('Base name for resources')
param appName string = 'onlynines'

@description('Globally unique web app name')
param webAppName string = '${appName}-${uniqueString(resourceGroup().id)}'

param location string = resourceGroup().location

@secure()
param pgPassword string

param pgAdmin string = 'onlynines_admin'

@description('Monthly budget in USD — alerts only, Azure has no native kill switch')
param budgetAmount int = 60

@description('Where budget alerts land')
param alertEmail string = 'pawelsiwek@gmail.com'

// ---------- App Service (B1, single instance, single zone — on purpose) ----------
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
    zoneRedundant: false // yes, really. The Resiliency Agent will complain. That's the show.
  }
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false // B1 penny-pinching; cold starts are content too
      appSettings: [
        {
          name: 'POSTGRES_CONNECTION'
          value: 'Host=${pg.properties.fullyQualifiedDomainName};Database=onlynines;Username=${pgAdmin};Password=${pgPassword};SSL Mode=Require;Trust Server Certificate=true'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// ---------- PostgreSQL Flexible Server (Burstable, no HA — on purpose) ----------
resource pg 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${appName}-pg-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: pgAdmin
    administratorLoginPassword: pgPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled' // cost fuse: the disk will fill before the bill does
    }
    highAvailability: {
      mode: 'Disabled' // the agent's favorite finding
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
  }
}

resource pgDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: pg
  name: 'onlynines'
}

resource pgFirewallAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: pg
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------- Cost fuse: budget with escalating alerts (50/80/100% + forecast) ----------
resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: 'onlynines-monthly'
  properties: {
    category: 'Cost'
    amount: budgetAmount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '2026-07-01T00:00:00Z'
      endDate: '2030-12-31T00:00:00Z'
    }
    notifications: {
      at50percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
      at80percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 80
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
      at100percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
      forecastedOverrun: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 110
        contactEmails: [alertEmail]
        thresholdType: 'Forecasted'
      }
    }
  }
}

output webAppName string = web.name
output webAppHost string = web.properties.defaultHostName
output pgHost string = pg.properties.fullyQualifiedDomainName
