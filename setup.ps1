# login
Write-Host "Sign in to Microsoft 365..."
npx -p @pnp/cli-microsoft365 -- m365 login --authType browser

# create AAD app
Write-Host "Creating AAD app..."
$appInfo=$(npx -p @pnp/cli-microsoft365 -- m365 entra app add --name "Aspire Graph Connector" --withSecret --apisApplication "https://graph.microsoft.com/ExternalConnection.ReadWrite.OwnedBy, https://graph.microsoft.com/ExternalItem.ReadWrite.OwnedBy" --grantAdminConsent --output json)

# write app to env.js
Write-Host "Writing app to env.js..."
New-Item -ItemType File -Path "./GraphConnector.Service.Queue/" -Name "appsettings.json" -Value "$($appInfo | Out-String)" -Force

Write-Host "DONE"
