#!/usr/bin/env bash
# Fix Azure Entra federated identity credentials for GitHub Actions OIDC.
#
# New GitHub repos (after 2026-07-15) emit immutable OIDC subjects:
#   repo:OWNER@OWNER_ID/REPO@REPO_ID:ref:refs/heads/main
# Azure Portal continuous-deploy often registers the classic subject instead:
#   repo:OWNER/REPO:ref:refs/heads/main
# That mismatch causes AADSTS700213 and blocks Container Apps deploy.
#
# Prerequisites: Azure CLI logged in with permission to manage the app registration
# that backs LOCATIONSERVICE_AZURE_CLIENT_ID (Application Administrator / owner).
#
# Usage:
#   az login
#   export APP_ID=<LOCATIONSERVICE_AZURE_CLIENT_ID>   # optional; auto-discovered if unset
#   ./scripts/fix-oidc-federated-credential.sh

set -euo pipefail

OWNER="${OWNER:-bihiya}"
REPO="${REPO:-tripwalaah-location-service}"
OWNER_ID="${OWNER_ID:-55905431}"
REPO_ID="${REPO_ID:-1319416454}"
BRANCH="${BRANCH:-main}"

IMMUTABLE_SUBJECT="repo:${OWNER}@${OWNER_ID}/${REPO}@${REPO_ID}:ref:refs/heads/${BRANCH}"
CLASSIC_SUBJECT="repo:${OWNER}/${REPO}:ref:refs/heads/${BRANCH}"
CREDENTIAL_NAME="${CREDENTIAL_NAME:-github-${REPO}-${BRANCH}-immutable}"

echo "Target immutable subject:"
echo "  ${IMMUTABLE_SUBJECT}"
echo

if [ -z "${APP_ID:-}" ]; then
  echo "APP_ID not set — searching app registrations for GitHub federated credentials..."
  mapfile -t CANDIDATES < <(
    az ad app list --query "[].{id:id, appId:appId, displayName:displayName}" -o tsv 2>/dev/null | head -50 || true
  )
  if [ "${#CANDIDATES[@]}" -eq 0 ]; then
    echo "Could not list app registrations. Set APP_ID to LOCATIONSERVICE_AZURE_CLIENT_ID."
    exit 1
  fi

  MATCHED=""
  while IFS=$'\t' read -r object_id app_id display_name; do
    [ -z "${object_id}" ] && continue
    subjects="$(az ad app federated-credential list --id "$object_id" --query "[].subject" -o tsv 2>/dev/null || true)"
    if echo "$subjects" | grep -Eq "tripwalaah-location-service|${REPO}"; then
      echo "Found matching app: ${display_name} (appId=${app_id})"
      APP_ID="$app_id"
      OBJECT_ID="$object_id"
      MATCHED=1
      break
    fi
  done <<< "$(printf '%s\n' "${CANDIDATES[@]}")"

  if [ -z "${MATCHED}" ]; then
    echo "No app registration with a ${REPO} federated credential was found."
    echo "Re-run with: export APP_ID=<LOCATIONSERVICE_AZURE_CLIENT_ID>"
    exit 1
  fi
else
  OBJECT_ID="$(az ad app show --id "$APP_ID" --query id -o tsv)"
fi

echo "Using app object id: ${OBJECT_ID}"
echo
echo "Existing federated credentials:"
az ad app federated-credential list --id "$OBJECT_ID" -o table
echo

EXISTING="$(az ad app federated-credential list --id "$OBJECT_ID" --query "[?subject=='${IMMUTABLE_SUBJECT}'].name" -o tsv)"
if [ -n "$EXISTING" ]; then
  echo "Immutable subject credential already present (${EXISTING}). Nothing to create."
else
  tmp="$(mktemp)"
  cat >"$tmp" <<EOF
{
  "name": "${CREDENTIAL_NAME}",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "${IMMUTABLE_SUBJECT}",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

  echo "Creating federated credential ${CREDENTIAL_NAME}..."
  az ad app federated-credential create --id "$OBJECT_ID" --parameters @"$tmp"
  rm -f "$tmp"
  echo "Created."
fi

echo
echo "Optional cleanup: remove classic mutable subject if present:"
echo "  subject=${CLASSIC_SUBJECT}"
CLASSIC_NAME="$(az ad app federated-credential list --id "$OBJECT_ID" --query "[?subject=='${CLASSIC_SUBJECT}'].name" -o tsv)"
if [ -n "$CLASSIC_NAME" ]; then
  echo "Found classic credential: ${CLASSIC_NAME}"
  echo "Delete after deploy succeeds:"
  echo "  az ad app federated-credential delete --id ${OBJECT_ID} --federated-credential-id ${CLASSIC_NAME}"
fi

echo
echo "Done. Re-run GitHub Action: Deploy to Azure Container Apps"
