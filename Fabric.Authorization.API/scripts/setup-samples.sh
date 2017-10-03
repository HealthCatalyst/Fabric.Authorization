#!/bin/bash

secret=$1

if [ $2 ]; then
	identitybaseurl=$2
fi

if [ $3 ]; then
	authorizationbaseurl=$3
fi

if ! [ $identitybaseurl ]; then
	identitybaseurl=http://localhost:5001
fi

if ! [ $authorizationbaseurl ]; then
	authorizationbaseurl=http://localhost:5004
fi

echo "getting access token for installer..."
accesstokenresponse=$(curl $identitybaseurl/connect/token --data "client_id=fabric-installer&grant_type=client_credentials" --data-urlencode "client_secret=$secret")
echo $accesstokenresponse
accesstoken=$(echo $accesstokenresponse | grep -oP '(?<="access_token":")[^"]*')
echo ""

echo "configuring Fabric.Authorization for samples..."
echo "setting up clients..."
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"id\": \"fabric-mvcsample\", \"name\": \"Sample Fabric MVC Client\", \"topLevelSecurableItem\": { \"name\":\"fabric-mvcsample\"}}" $authorizationbaseurl/clients/
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"id\": \"fabric-angularsample\", \"name\": \"Sample Fabric Angular Client\", \"topLevelSecurableItem\": { \"name\":\"fabric-angularsample\"}}" $authorizationbaseurl/clients/
echo ""

viewerRole="FABRIC\\\Health Catalyst Viewer"
editorRole="FABRIC\\\Health Catalyst Editor"
echo "setting up sample groups..."
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"id\": \"$viewerRole\", \"groupName\": \"$viewerRole\", \"groupSource\": \"custom\"}" $authorizationbaseurl/groups/ 
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"id\": \"$editorRole\", \"groupName\": \"$editorRole\", \"groupSource\": \"custom\"}" $authorizationbaseurl/groups/
