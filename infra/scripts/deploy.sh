#!/bin/sh
resourceGroupName="<RSG>"
location="Australia East"

##Create resource group
az group create --name $resourceGroupName --location $location

###########################

##Deploy resource group and provision resources
az deployment group create \
    --resource-group $resourceGroupName \
    --template-file "simple-storage-deploy.bicep" \
    --parameters "simple-storage.parameters.json"  #--what-if

##Cleanup Resourcegroup
az group delete --name $resourceGroupName --yes --no-wait