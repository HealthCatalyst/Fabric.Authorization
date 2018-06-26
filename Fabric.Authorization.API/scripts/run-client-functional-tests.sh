#!/bin/bash

docker stop authz-client-functional-authorization
docker rm authz-client-functional-authorization
docker stop authz-client-functional-identity
docker rm authz-client-functional-identity
docker network rm authz-client-functional-tests

docker pull healthcatalyst/fabric.identity
docker pull healthcatalyst/fabric.authorization

docker network create authz-client-functional-tests

docker run -d --name authz-client-functional-identity \
	-p 5001:5001 \
	-e "HostingOptions__StorageProvider=InMemory" \
	-e "HostingOptions__AllowUnsafeEval=true" \
	-e "IssuerUri=http://authz-client-functional-identity:5001" \
	-e "IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://authz-client-functional-identity:5001" \
	--network="authz-client-functional-tests" \
	healthcatalyst/fabric.identity
echo "started identity"
sleep 3

curl -sSL https://raw.githubusercontent.com/HealthCatalyst/Fabric.Identity/master/Fabric.Identity.API/scripts/setup-samples.sh > identity-setup-samples.sh
output=$(. identity-setup-samples.sh)
installerSecret=$(echo $output | grep -oP '(?<="installerSecret":")[^"]*')
authClientSecret=$(echo $output | grep -oP '(?<="authClientSecret":")[^"]*')

export FABRIC_INSTALLER_SECRET=$installerSecret
echo $installerSecret
echo $authClientSecret

docker run -d --name authz-client-functional-authorization \
	-p 5004:5004 \
	-e STORAGEPROVIDER=InMemory \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://authz-client-functional-identity:5001/ \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__CLIENTSECRET=$authClientSecret \
	--network="authz-client-functional-tests" \
	healthcatalyst/fabric.authorization
echo "started authorization"
sleep 5 

cd scripts
(. setup-samples.sh $installerSecret)

cd ../../Fabric.Authorization.Client.FunctionalTests
export FABRIC_IDENTITY_URL=http://localhost:5001/
export FABRIC_AUTH_URL=http://localhost:5004/
export FABRIC_AUTH_SECRET=$authClientSecret
echo "running tests"
dotnet test ./Fabric.Authorization.Client.FunctionalTests.csproj

cd ../Fabric.Authorization.API/scripts
rm identity-setup-samples.sh

docker stop authz-client-functional-identity
docker rm authz-client-functional-identity
docker stop authz-client-functional-authorization
docker rm authz-client-functional-authorization
docker network rm authz-client-functional-tests
