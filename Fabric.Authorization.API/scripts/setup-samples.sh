#!/bin/bash

secret=$1

echo "getting access token for installer..."
accesstokenresponse=$(curl http://localhost:5001/connect/token --data "client_id=fabric-installer&grant_type=client_credentials" --data-urlencode "client_secret=$secret")
echo $accesstokenresponse
accesstoken=$(echo $accesstokenresponse | grep -oP '(?<="access_token":")[^"]*')
echo ""

echo "configuring Fabric.Authorization for samples..."
echo "setting up clients..."
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -H "fabric-end-user-subject-id: test" -d "{ \"id\": \"fabric-mvcsample\", \"name\": \"Sample Fabric MVC Client\", \"topLevelSecurableItem\": { \"name\":\"fabric-mvcsample\"}}" http://localhost:5004/clients/
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -H "fabric-end-user-subject-id: test" -d "{ \"id\": \"fabric-angularsample\", \"name\": \"Sample Fabric Angular Client\", \"topLevelSecurableItem\": { \"name\":\"fabric-angularsample\"}}" http://localhost:5004/clients/
echo ""

viewerRole="FABRIC\\\Health Catalyst Viewer"
editorRole="FABRIC\\\Health Catalyst Editor"
echo "setting up sample groups..."
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -H "fabric-end-user-subject-id: test" -d "{ \"id\": \"$viewerRole\", \"groupName\": \"$viewerRole\"}" http://localhost:5004/groups/ 
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -H "fabric-end-user-subject-id: test" -d "{ \"id\": \"$editorRole\", \"groupName\": \"$editorRole\"}" http://localhost:5004/groups/
