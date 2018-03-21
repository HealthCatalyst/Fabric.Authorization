#!/bin/bash

docker stop authz-functional-authorization
docker rm authz-functional-authorization
docker stop authz-functional-identity
docker rm authz-functional-identity
docker network rm authz-functional-tests

docker pull healthcatalyst/fabric.identity

docker network create authz-functional-tests

docker run -d --name authz-functional-identity \
	-p 5001:5001 \
	-e "HostingOptions__StorageProvider=InMemory" \
	-e "HostingOptions__AllowUnsafeEval=true" \
	-e "IssuerUri=http://authz-functional-identity:5001" \
	-e "IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://authz-functional-identity:5001" \
	--network="authz-functional-tests" \
	healthcatalyst/fabric.identity
echo "started identity"
sleep 3

output=$(curl -sSL https://raw.githubusercontent.com/HealthCatalyst/Fabric.Identity/master/Fabric.Identity.API/scripts/setup-samples.sh | sh /dev/stdin http://localhost:5001)
installerSecret=$(echo $output | grep -oP '(?<="installerSecret":")[^"]*')
authClientSecret=$(echo $output | grep -oP '(?<="authClientSecret":")[^"]*')

export FABRIC_INSTALLER_SECRET=$installerSecret
echo $installerSecret
echo $authClientSecret
cd ..
dotnet publish -o obj/Docker/publish
docker build -t authorization.functional.api .

docker run -d --name authz-functional-authorization \
	-p 5004:5004 \
	-e STORAGEPROVIDER=InMemory \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://authz-functional-identity:5001/ \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__CLIENTSECRET=$authClientSecret \
	--network="authz-functional-tests" \
	authorization.functional.api
echo "started authorization"
sleep 10 

./scripts/setup-samples.sh $installerSecret

cd ../Fabric.Authorization.FunctionalTests
npm install
npm test

docker stop authz-functional-identity
docker rm authz-functional-identity
docker stop authz-functional-authorization
docker rm authz-functional-authorization
docker network rm authz-functional-tests
