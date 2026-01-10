# Azure Web App Deployment with GitHub Actions - Complete Guide

## Overview

This guide covers setting up CI/CD pipeline to deploy a .NET Web API to Azure App Service using GitHub Actions with Azure Service Principal authentication.

## Flow Summary

1. **Code Push** → GitHub repository (main branch)
2. **GitHub Actions Triggered** → Builds .NET application
3. **Azure Authentication** → Using Service Principal credentials
4. **Deployment** → Publishes to Azure Web App

---

## Prerequisites

- Azure subscription
- Azure CLI installed
- GitHub repository
- .NET Web API project
- Azure Web App created

---

## Step-by-Step Setup

### 1. Install and Login to Azure CLI

```bash
# Login to Azure
az login

# Verify your account
az account show
```

### 2. Find Your Azure Resources

```bash
# List all resource groups
az group list --output table

# List all web apps (to find your app and resource group)
az webapp list --output table

# Get your subscription ID
az account show --query id --output tsv
```

### 3. Create Service Principal for GitHub Actions

Replace the placeholders with your actual values:

```bash
# Create service principal with contributor role
az ad sp create-for-rbac \
  --name "github-actions-cicd" \
  --role contributor \
  --scopes /subscriptions/<YOUR-SUBSCRIPTION-ID>/resourceGroups/<YOUR-RESOURCE-GROUP> \
  --sdk-auth
```

**Example with actual values:**
```bash
az ad sp create-for-rbac \
  --name "github-actions-cicd" \
  --role contributor \
  --scopes /subscriptions/12345678-1234-1234-1234-123456789abc/resourceGroups/MyResourceGroup \
  --sdk-auth
```

**Output will look like:**
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**⚠️ IMPORTANT:** Copy this entire JSON output immediately. The client secret cannot be retrieved again.

### 4. Add Secret to GitHub Repository

1. Go to your GitHub repository
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `AZURE_CREDENTIALS`
5. Value: Paste the entire JSON output from step 3
6. Click **Add secret**

### 5. Create GitHub Actions Workflow

Create file: `.github/workflows/azure-deploy.yml`

```yaml
name: Build and Deploy Web API

on:
  push:
    branches:
      - main

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Publish
        run: dotnet publish -c Release -o ./publish

      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Azure WebApp
        uses: azure/webapps-deploy@v2
        with:
          app-name: UserMicroservicesApi
          slot-name: production
          package: ./publish

      - name: Logout from Azure
        run: az logout
```

### 6. Commit and Push

```bash
git add .github/workflows/azure-deploy.yml
git commit -m "Add Azure deployment workflow"
git push origin main
```

The workflow will automatically trigger and deploy your application.

---

## Common Issues and Resolutions

### Issue 1: Authentication Error - Missing client-id and tenant-id

**Error Message:**
```
Login failed with Error: Using auth-type: SERVICE_PRINCIPAL. Not all values are present. 
Ensure 'client-id' and 'tenant-id' are supplied.
```

**Cause:** 
- `AZURE_CREDENTIALS` secret is not set correctly in GitHub
- Secret value is not in proper JSON format
- Using wrong authentication method in workflow

**Resolution:**

1. Verify the secret exists in GitHub (Settings → Secrets and variables → Actions)
2. Ensure you used `--sdk-auth` flag when creating service principal
3. Recreate the service principal if needed:
   ```bash
   az ad sp create-for-rbac \
     --name "github-actions-cicd" \
     --role contributor \
     --scopes /subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP> \
     --sdk-auth
   ```
4. Copy the ENTIRE JSON output and update the GitHub secret

### Issue 2: Invalid .NET Version

**Error Message:**
```
Version 10.0.x was not found
```

**Cause:** 
.NET 10 doesn't exist yet

**Resolution:**
Update workflow to use valid .NET version:
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'  # or '9.0.x'
```

### Issue 3: Insufficient Permissions

**Error Message:**
```
Authorization failed or The client does not have authorization to perform action
```

**Cause:** 
Service principal doesn't have sufficient permissions

**Resolution:**

1. Verify service principal has Contributor role:
   ```bash
   az role assignment list --assignee <CLIENT-ID> --output table
   ```

2. Add contributor role if missing:
   ```bash
   az role assignment create \
     --assignee <CLIENT-ID> \
     --role Contributor \
     --scope /subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP>
   ```

### Issue 4: Web App Not Found

**Error Message:**
```
Web app 'DemoCICDWebApi' doesn't exist
```

**Cause:** 
Web app name in workflow doesn't match actual Azure resource

**Resolution:**

1. List your web apps:
   ```bash
   az webapp list --output table
   ```

2. Update `app-name` in workflow with correct name

### Issue 5: Deployment Slot Error

**Error Message:**
```
Slot 'production' not found
```

**Cause:** 
Deployment slots are not enabled for the app service plan

**Resolution:**

Either remove the slot parameter:
```yaml
- name: Deploy to Azure WebApp
  uses: azure/webapps-deploy@v2
  with:
    app-name: DemoCICDWebApi
    package: ./publish
```

Or create a deployment slot:
```bash
az webapp deployment slot create \
  --name DemoCICDWebApi \
  --resource-group <RESOURCE-GROUP> \
  --slot production
```

### Issue 6: Package Path Not Found

**Error Message:**
```
Error: Package or folder path not found
```

**Cause:** 
Published files are not in the expected location

**Resolution:**

Ensure publish path matches in both commands:
```yaml
- name: Publish
  run: dotnet publish -c Release -o ./publish

- name: Deploy to Azure WebApp
  uses: azure/webapps-deploy@v2
  with:
    package: ./publish  # Must match output path above
```

---

## Alternative: Using Federated Credentials (More Secure)

Instead of storing client secrets, you can use OpenID Connect (OIDC) federated credentials:

### Setup Federated Credentials

```bash
# Get the application object ID
APP_ID=$(az ad sp list --display-name "github-actions-cicd" --query "[0].appId" -o tsv)
OBJECT_ID=$(az ad app show --id $APP_ID --query id -o tsv)

# Create federated credential
az ad app federated-credential create \
  --id $OBJECT_ID \
  --parameters '{
    "name": "github-federated-credential",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<GITHUB-ORG>/<GITHUB-REPO>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### Update Workflow for Federated Auth

```yaml
- name: Login to Azure
  uses: azure/login@v1
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Add Individual Secrets to GitHub

- `AZURE_CLIENT_ID` - Application (client) ID
- `AZURE_TENANT_ID` - Directory (tenant) ID  
- `AZURE_SUBSCRIPTION_ID` - Subscription ID

---

## Verification Commands

```bash
# Check if service principal exists
az ad sp list --display-name "github-actions-cicd" --output table

# List role assignments for service principal
az role assignment list --assignee <CLIENT-ID> --output table

# View web app details
az webapp show --name DemoCICDWebApi --resource-group <RESOURCE-GROUP>

# View deployment logs
az webapp log tail --name DemoCICDWebApi --resource-group <RESOURCE-GROUP>
```

---

## Cleanup (Optional)

To remove the service principal when no longer needed:

```bash
# List service principals
az ad sp list --display-name "github-actions-cicd" --query "[].{Name:displayName, AppId:appId}" -o table

# Delete service principal
az ad sp delete --id <APP-ID>
```

---

## Best Practices

1. ✅ Use specific resource group scope instead of subscription-wide access
2. ✅ Use federated credentials instead of client secrets when possible
3. ✅ Rotate client secrets regularly (if using secrets)
4. ✅ Use deployment slots for zero-downtime deployments
5. ✅ Enable Application Insights for monitoring
6. ✅ Set up branch protection rules to control deployments
7. ✅ Use environment secrets for different environments (dev, staging, prod)

---

## Additional Resources

- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
- [Azure Web Apps Deploy Action](https://github.com/Azure/webapps-deploy)
- [Service Principal Authentication](https://docs.microsoft.com/azure/developer/github/connect-from-azure)

---

## Quick Reference

| Component | Purpose |
|-----------|---------|
| Service Principal | Identity that GitHub Actions uses to authenticate with Azure |
| AZURE_CREDENTIALS | GitHub secret containing all Azure authentication info |
| azure/login@v1 | GitHub Action that authenticates with Azure |
| azure/webapps-deploy@v2 | GitHub Action that deploys to Azure Web Apps |
| --sdk-auth | Azure CLI flag that outputs credentials in correct JSON format |

---

**Last Updated:** January 2026
