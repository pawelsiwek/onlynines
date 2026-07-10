// Second-pass SSL binding: hostname binding must exist before the managed cert,
// and the cert must exist before SNI can be enabled — hence this module.
param webAppName string
param hostname string
param thumbprint string

resource binding 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = {
  name: '${webAppName}/${hostname}'
  properties: {
    sslState: 'SniEnabled'
    thumbprint: thumbprint
  }
}
