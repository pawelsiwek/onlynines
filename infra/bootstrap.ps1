# OnlyNines one-time bootstrap: resource group + GitHub OIDC federation + repo secrets.
# Run once, locally, logged in to both `az` and `gh`. GitHub Actions handles everything after.
param(
    [string]$SubscriptionId = 'b531ce38-fac4-4b5b-aa57-09dac1d89b35',
    [string]$Repo = 'pawelsiwek/onlynines',
    [string]$ResourceGroup = 'rg-onlynines-prod',
    [string]$Location = 'northeurope',
    [string]$AppRegName = 'onlynines-github-deploy'
)

$ErrorActionPreference = 'Stop'

az account set --subscription $SubscriptionId
Write-Host "==> Resource group $ResourceGroup in $Location"
az group create --name $ResourceGroup --location $Location --output none

Write-Host "==> App registration + service principal"
$app = az ad app create --display-name $AppRegName | ConvertFrom-Json
az ad sp create --id $app.appId --output none 2>$null

Write-Host "==> Federated credential for GitHub OIDC (main branch)"
$fedParams = @{
    name      = 'onlynines-main'
    issuer    = 'https://token.actions.githubusercontent.com'
    subject   = "repo:${Repo}:ref:refs/heads/main"
    audiences = @('api://AzureADTokenExchange')
} | ConvertTo-Json -Compress
$fedParams | Out-File -Encoding utf8 fed.json
az ad app federated-credential create --id $app.id --parameters fed.json --output none 2>$null
Remove-Item fed.json

Write-Host "==> Contributor on the resource group (least privilege: RG scope only)"
az role assignment create `
    --assignee $app.appId `
    --role Contributor `
    --scope "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup" --output none 2>$null

Write-Host "==> GitHub repo secrets"
$tenantId = az account show --query tenantId -o tsv
$pgPassword = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 28 | ForEach-Object { [char]$_ }) + '!x9'
gh secret set AZURE_CLIENT_ID       --repo $Repo --body $app.appId
gh secret set AZURE_TENANT_ID       --repo $Repo --body $tenantId
gh secret set AZURE_SUBSCRIPTION_ID --repo $Repo --body $SubscriptionId
gh secret set PG_PASSWORD           --repo $Repo --body $pgPassword

Write-Host ""
Write-Host "Done. Push to main (or run the 'deploy' workflow) and GitHub Actions will provision + deploy."
Write-Host "PG password is stored ONLY as a GitHub secret — treat the repo secrets as the source of truth."
