#!/bin/bash

sourceFile=$1

echo $sourceFile

clientSecretJson=$(<$sourceFile)
echo $clientSecretJson

installersecret=$(echo $clientSecretJson | jq -r .installerSecret) 
authorizationclientsecret=$(echo $clientSecretJson | jq -r .authClientSecret) 

echo $installersecret
echo $authorizationclientsecret

echo "##vso[task.setvariable variable=FABRIC_INSTALLER_SECRET;]$installersecret"
echo "##vso[task.setvariable variable=AUTH_CLIENT_SECRET;]$authorizationclientsecret"

