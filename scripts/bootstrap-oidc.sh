#!/usr/bin/env bash
set -euo pipefail

REPO_OWNER="fatihkayas"
REPO_NAME="test3"
BRANCH="main"

RG_NAME="rg-test3-aca"
APP_NAME="gha-${REPO_NAME}-aca-oidc"

echo "Reading Azure context..."
SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)

echo "Creating or reusing App registration..."
APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv || true)

if [[ -z "$APP_ID" ]]; then
  APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
  echo "App created"
else
  echo "App already exists"
fi

echo "Ensuring Service Principal..."
SP_ID=$(az ad sp list --filter "appId eq '$APP_ID'" --query "[0].id" -o tsv || true)
if [[ -z "$SP_ID" ]]; then
  SP_ID=$(az ad sp create --id "$APP_ID" --query id -o tsv)
fi

echo "Creating federated credential..."
cat > federated.json << JSON
{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:${REPO_OWNER}/${REPO_NAME}:ref:refs/heads/${BRANCH}",
  "audiences": ["api://AzureADTokenExchange"]
}
JSON

EXISTS=$(az ad app federated-credential list --id "$APP_ID" --query "[?name=='github-main'] | length(@)" -o tsv || echo "0")
if [[ "$EXISTS" == "0" ]]; then
  az ad app federated-credential create --id "$APP_ID" --parameters federated.json
fi

echo "Assigning Contributor role on Resource Group..."
RG_ID=$(az group show -n "$RG_NAME" --query id -o tsv 2>/dev/null || true)
if [[ -z "$RG_ID" ]]; then
  echo "Resource Group $RG_NAME does not exist. Create it first:"
  echo "az group create -n $RG_NAME -l westeurope"
  exit 1
fi

az role assignment create \
  --assignee-object-id "$SP_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$RG_ID" || true

echo ""
echo "==== ADD THESE TO GITHUB SECRETS ===="
echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$TENANT"
echo "AZURE_SUBSCRIPTION_ID=$SUB"
echo "==================================="
