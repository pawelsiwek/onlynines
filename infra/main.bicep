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

@description('Cloudflare Web Analytics beacon token (empty = beacon disabled)')
param cfAnalyticsToken string = ''

@description('Public URL pinged by the availability test')
param publicUrl string = 'https://onlynines.app/'

@description('Custom domains with App Service managed certificates. Requires DNS (A/CNAME + asuid TXT) to exist first — pass [] on virgin environments.')
param customDomains array = [
  'onlynines.app'
  'www.onlynines.app'
]

@description('Lock the origin so ONLY Cloudflare edge IPs reach App Service. Prod is permanently behind CF orange-cloud, so this defaults true — deploys keep the lockdown. A virgin/non-CF environment MUST pass restrictToCloudflare=false or it locks out all direct traffic and goes dark. See docs/cloudflare.md.')
param restrictToCloudflare bool = true

@description('Cloudflare edge IP ranges (IPv4 + IPv6). Refresh from https://www.cloudflare.com/ips/ — Cloudflare changes these occasionally.')
param cloudflareIpRanges array = [
  '173.245.48.0/20'
  '103.21.244.0/22'
  '103.22.200.0/22'
  '103.31.4.0/22'
  '141.101.64.0/18'
  '108.162.192.0/18'
  '190.93.240.0/20'
  '188.114.96.0/20'
  '197.234.240.0/22'
  '198.41.128.0/17'
  '162.158.0.0/15'
  '104.16.0.0/13'
  '104.24.0.0/14'
  '172.64.0.0/13'
  '131.0.72.0/22'
  '2400:cb00::/32'
  '2606:4700::/32'
  '2803:f800::/32'
  '2405:b500::/32'
  '2405:8100::/32'
  '2a06:98c0::/29'
  '2c0f:f248::/32'
]

// Allow-list of Cloudflare ranges; anything unmatched is denied by ipSecurityRestrictionsDefaultAction.
var cfIpRestrictions = [for (cidr, i) in cloudflareIpRanges: {
  ipAddress: cidr
  action: 'Allow'
  priority: 100 + i
  name: 'cloudflare-${i}'
  description: 'Cloudflare edge range'
}]

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
      // Origin lockdown: when restrictToCloudflare is on, only CF edge IPs get in.
      // SCM (Kudu) stays open so GitHub Actions can still deploy.
      ipSecurityRestrictionsDefaultAction: restrictToCloudflare ? 'Deny' : 'Allow'
      ipSecurityRestrictions: restrictToCloudflare ? cfIpRestrictions : []
      scmIpSecurityRestrictionsUseMain: false
      appSettings: [
        {
          name: 'POSTGRES_CONNECTION'
          value: 'Host=${pg.properties.fullyQualifiedDomainName};Database=onlynines;Username=${pgAdmin};Password=${pgPassword};SSL Mode=Require;Trust Server Certificate=true'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CF_ANALYTICS_TOKEN'
          value: cfAnalyticsToken
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

// ---------- Custom domains + managed certs (three-step dance, see ssl-bind.bicep) ----------
resource hostBinding 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = [for domain in customDomains: {
  parent: web
  name: domain
  properties: {
    siteName: web.name
    hostNameType: 'Verified'
  }
}]

resource managedCert 'Microsoft.Web/certificates@2023-12-01' = [for (domain, i) in customDomains: {
  name: 'cert-${replace(domain, '.', '-')}'
  location: location
  properties: {
    serverFarmId: plan.id
    canonicalName: domain
  }
  dependsOn: [hostBinding[i]]
}]

module sslBind 'ssl-bind.bicep' = [for (domain, i) in customDomains: {
  name: 'ssl-${replace(domain, '.', '-')}'
  params: {
    webAppName: web.name
    hostname: domain
    thumbprint: managedCert[i].properties.thumbprint
  }
}]

// ---------- Observability: OTEL -> Azure Monitor (free tier covers this app 100x over) ----------
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

// "Did it die?" — pinged from 3 regions every 5 minutes.
resource ping 'Microsoft.Insights/webtests@2022-06-15' = {
  name: '${appName}-ping'
  location: location
  tags: {
    'hidden-link:${appInsights.id}': 'Resource'
  }
  properties: {
    SyntheticMonitorId: '${appName}-ping'
    Name: 'onlynines availability'
    Enabled: true
    Frequency: 300
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      { Id: 'emea-nl-ams-azr' }
      { Id: 'emea-gb-db3-azr' }
      { Id: 'us-va-ash-azr' }
    ]
    Request: {
      RequestUrl: publicUrl
      HttpVerb: 'GET'
      ParseDependentRequests: false
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
      SSLCertRemainingLifetimeCheck: 7
    }
  }
}

resource alertGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${appName}-alerts'
  location: 'Global'
  properties: {
    groupShortName: 'onlynines'
    enabled: true
    emailReceivers: [
      {
        name: 'owner'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appName}-down'
  location: 'global'
  properties: {
    severity: 1
    enabled: true
    scopes: [ping.id, appInsights.id]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: ping.id
      componentId: appInsights.id
      failedLocationCount: 2
    }
    actions: [
      { actionGroupId: alertGroup.id }
    ]
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
