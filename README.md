# whatsmyipaddress
A container that will echo back relevant information about any incoming request, such as IP address, etc.

```bash

LOCATION=eastus2
BASENAME=cdw-lumentesting-20210215
REGISTRY_NAME=cdwms
REGISTRY_PASSWORD=XXXX

az group create -n $BASENAME -l $LOCATION


az network vnet create \
  --name $BASENAME-vnet \
  --resource-group $BASENAME \
  --address-prefixes 10.0.0.0/8

az network vnet subnet create \
  --name ingress-subnet \
  --vnet-name $BASENAME-vnet \
  --resource-group $BASENAME \
  --address-prefixes 10.0.0.0/16

az network vnet subnet create \
  --name cplane-subnet \
  --vnet-name $BASENAME-vnet \
  --resource-group $BASENAME \
  --address-prefixes 10.10.0.0/16

az network vnet subnet create \
  --name app-subnet \
  --vnet-name $BASENAME-vnet \
  --resource-group $BASENAME \
  --address-prefixes 10.11.0.0/16

az network vnet subnet create \
  --name bastion-subnet \
  --vnet-name $BASENAME-vnet \
  --resource-group $BASENAME \
  --address-prefixes 10.99.0.0/16

# VNET_ID=`az network vnet show -g $BASENAME -n $BASENAME-vnet --query id --output tsv`
CPLANE_SUBNET=`az network vnet subnet list --resource-group $BASENAME --vnet-name $BASENAME-vnet --query "[?name=='cplane-subnet'].id" --output tsv`
APP_SUBNET=`az network vnet subnet list --resource-group $BASENAME --vnet-name $BASENAME-vnet --query "[?name=='app-subnet'].id" --output tsv`


az monitor log-analytics workspace create \
  --resource-group $BASENAME \
  --workspace-name $BASENAME-wksp

WORKSPACE_CLIENT_ID=`az monitor log-analytics workspace show --query customerId -g $BASENAME -n $BASENAME-wksp -o tsv | tr -d '[:space:]'`
WORKSPACE_CLIENT_SECRET=`az monitor log-analytics workspace get-shared-keys --query primarySharedKey -g $BASENAME -n $BASENAME-wksp -o tsv | tr -d '[:space:]'`


az extension add --source https://workerappscliextension.blob.core.windows.net/azure-cli-extension/containerapp-0.2.2-py2.py3-none-any.whl
az provider register --namespace Microsoft.Web

az containerapp env create \
  --name $BASENAME-env \
  --resource-group $BASENAME \
  --location $LOCATION \
  --internal-only \
  --app-subnet-resource-id $APP_SUBNET \
  --controlplane-subnet-resource-id $CPLANE_SUBNET \
  --logs-workspace-id $WORKSPACE_CLIENT_ID \
  --logs-workspace-key $WORKSPACE_CLIENT_SECRET

ENV_DOMAIN=`az containerapp env show -n $BASENAME-env -g $BASENAME --query defaultDomain --out json | tr -d '"'`
ENV_STATIC_IP=`az containerapp env show -n $BASENAME-env -g $BASENAME --query staticIp --out json | tr -d '"'`

# Setup DNS
az network private-dns zone create \
  --resource-group $BASENAME \
  --name $ENV_DOMAIN

az network private-dns link vnet create \
  -g $BASENAME \
  -n $BASENAME-vnet \
  -v $BASENAME-vnet \
  --zone-name $ENV_DOMAIN \
  --registration-enabled true

az network private-dns record-set a add-record \
  -g $BASENAME \
  -n "*" \
  --ipv4-address $ENV_STATIC_IP \
  --zone-name $ENV_DOMAIN


az containerapp create \
  --name $BASENAME-app \
  --resource-group $BASENAME \
  --environment $BASENAME-env \
  --image cdwms.azurecr.io/whatsmyip:latest \
  --registry-login-server cdwms.azurecr.io \
  --registry-username $REGISTRY_NAME \
  --registry-password $REGISTRY_PASSWORD \
  --ingress external \
  --target-port 5000 \
  --min-replicas 0 \
  --max-replicas 1




# Use bastion to troubleshoot
az container create \
  -n $BASENAME-aci \
  -g $BASENAME \
  -l $LOCATION \
  --image cwiederspan/bastion:latest \
  --vnet $BASENAME-vnet \
  --subnet bastion-subnet

az container exec \
  -n $BASENAME-aci \
  -g $BASENAME \
  --exec-command './bin/bash'


# How does this work with a VNET?
az apim create \
  --name $BASENAME-apim \
  --resource-group $BASENAME \
  --location $LOCATION \
  --publisher-name Microsoft \
  --publisher-email chwieder@microsoft.com \
  --sku-name Developer \
  --virtual-network External
  --no-wait


```