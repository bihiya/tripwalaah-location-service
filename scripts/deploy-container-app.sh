#!/usr/bin/env bash
# One-time / manual deploy of tripwalaah-location-service to Azure Container Apps
# (Consumption / pay-as-you-go, scales to zero).
#
# Prerequisites: Azure CLI logged in (`az login`), Docker image already in ACR.
#
# Usage:
#   export ACR_NAME=myregistry
#   export RESOURCE_GROUP=tripwalaah-rg
#   export LOCATION=eastus
#   export MONGODB_URI='mongodb+srv://USER:PASS@cluster/tripwalaah'
#   ./scripts/deploy-container-app.sh

set -euo pipefail

ACR_NAME="${ACR_NAME:?Set ACR_NAME (short name, no .azurecr.io)}"
RESOURCE_GROUP="${RESOURCE_GROUP:?Set RESOURCE_GROUP}"
LOCATION="${LOCATION:-eastus}"
ENVIRONMENT_NAME="${ENVIRONMENT_NAME:-tripwalaah-env}"
APP_NAME="${APP_NAME:-tripwalaah-location}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
MONGODB_URI="${MONGODB_URI:?Set MONGODB_URI (Atlas or Azure Cosmos Mongo connection string)}"

ACR_LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)"
IMAGE="${ACR_LOGIN_SERVER}/tripwalaah-location-service:${IMAGE_TAG}"

echo "Ensuring resource group ${RESOURCE_GROUP}..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "Ensuring Container Apps environment ${ENVIRONMENT_NAME} (Consumption)..."
if ! az containerapp env show --name "$ENVIRONMENT_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az containerapp env create \
    --name "$ENVIRONMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none
fi

# Enable ACR admin (or use a pull SP / managed identity in production hardening)
az acr update --name "$ACR_NAME" --admin-enabled true --output none
ACR_USERNAME="$(az acr credential show --name "$ACR_NAME" --query username -o tsv)"
ACR_PASSWORD="$(az acr credential show --name "$ACR_NAME" --query passwords[0].value -o tsv)"

echo "Creating/updating Container App ${APP_NAME} <- ${IMAGE}"
if az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az containerapp secret set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --secrets "mongodb-uri=${MONGODB_URI}" \
    --output none

  az containerapp registry set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --server "$ACR_LOGIN_SERVER" \
    --username "$ACR_USERNAME" \
    --password "$ACR_PASSWORD" \
    --output none

  az containerapp update \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$IMAGE" \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 0 \
    --max-replicas 3 \
    --set-env-vars \
      "ASPNETCORE_ENVIRONMENT=Production" \
      "PORT=5000" \
      "API_PREFIX=/api" \
      "REDIS_ENABLED=false" \
      "KAFKA_ENABLED=false" \
      "SIGNALR_ENABLED=true" \
      "MONGODB_URI=secretref:mongodb-uri" \
    --output none
else
  az containerapp create \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --environment "$ENVIRONMENT_NAME" \
    --image "$IMAGE" \
    --target-port 5000 \
    --ingress external \
    --registry-server "$ACR_LOGIN_SERVER" \
    --registry-username "$ACR_USERNAME" \
    --registry-password "$ACR_PASSWORD" \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 0 \
    --max-replicas 3 \
    --secrets "mongodb-uri=${MONGODB_URI}" \
    --env-vars \
      "ASPNETCORE_ENVIRONMENT=Production" \
      "PORT=5000" \
      "API_PREFIX=/api" \
      "REDIS_ENABLED=false" \
      "KAFKA_ENABLED=false" \
      "SIGNALR_ENABLED=true" \
      "MONGODB_URI=secretref:mongodb-uri" \
    --output none
fi

FQDN="$(az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv)"
echo ""
echo "Deployed: https://${FQDN}"
echo "Health:  https://${FQDN}/health"
echo "API:     https://${FQDN}/api/locations"
echo ""
echo "Billing: Container Apps Consumption — scales to 0 when idle (pay for use)."
echo "MongoDB cost is separate (use Atlas free M0 or an existing cluster)."
