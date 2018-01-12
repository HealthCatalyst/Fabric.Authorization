#!/bin/bash
couchusername=$1
couchpassword=$2

docker stop authz-functional-couchdb
docker rm authz-functional-couchdb
docker volume rm authorization-func-db-data
docker stop authz-functional-authorization
docker rm authz-functional-authorization
docker stop authz-functional-identity
docker rm authz-functional-identity
docker network rm authz-functional-tests

docker pull healthcatalyst/fabric.identity

docker network create authz-functional-tests

docker run -d --name authz-functional-couchdb \
	-p 5984:5984 \
	-e COUCHDB_USER=$couchusername \
	-e COUCHDB_PASSWORD=$couchpassword \
	-v authorization-func-db-data://opt/couch/data \
	--network="authz-functional-tests" \
	healthcatalyst/fabric.docker.couchdb
echo "started couchdb"
sleep 15

docker run -d --name authz-functional-identity \
	-p 5001:5001 \
	-e "HostingOptions__StorageProvider=CouchDB" \
	-e "CouchDbSettings__Server=http://authz-functional-couchdb:5984" \
	-e "CouchDbSettings__Username=$couchusername" \
	-e "CouchDbSettings__Password=$couchpassword" \
	--network="authz-functional-tests" \
	healthcatalyst/fabric.identity
echo "started identity"
sleep 3

output=$(curl -sSL https://raw.githubusercontent.com/HealthCatalyst/Fabric.Identity/master/Fabric.Identity.API/scripts/setup-samples.sh | sh /dev/stdin http://localhost:5001)
installerSecret=$(echo $output | grep -oP '(?<="installerSecret":")[^"]*')
authApiSecret=$(echo $output | grep -oP '(?<="authApiSecret":")[^"]*')

export FABRIC_INSTALLER_SECRET=$installerSecret
export COUCHDBSETTINGS__USERNAME=$couchusername
export COUCHDBSETTINGS__PASSWORD=$couchpassword
echo $installerSecret
echo $authApiSecret
cd ..
dotnet publish -o obj/Docker/publish
docker build -t authorization.functional.api .

docker run -d --name authz-functional-authorization \
	-p 5004:5004 \
	-e COUCHDBSETTINGS__USERNAME=$couchusername \
	-e COUCHDBSETTINGS__PASSWORD=$couchpassword \
	-e COUCHDBSETTINGS__SERVER=http://authz-functional-couchdb:5984 \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://authz-functional-identity:5001 \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__CLIENTSECRET=$authApiSecret \
	--network="authz-functional-tests" \
	authorization.functional.api
echo "started authorization"
sleep 3

./scripts/setup-samples.sh $installerSecret

cd ../Fabric.Authorization.FunctionalTests
npm install
npm test

docker stop authz-functional-couchdb
docker rm authz-functional-couchdb
docker volume rm authorization-func-db-data
docker stop authz-functional-identity
docker rm authz-functional-identity
docker stop authz-functional-authorization
docker rm authz-functional-authorization
docker network rm authz-functional-tests
