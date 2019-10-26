#!/bin/bash

# add node setup steps
npm install
ng build

secret=$1

if ! [ $secret ]; then
  echo "Usage: 'setup-access-control.sh [fabric-installer-client-secret]'. You must supply the Fabric Installer client secret."
  exit
fi

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
accesstokenresponse=$(curl -v -k $identitybaseurl/connect/token --data "client_id=fabric-installer&grant_type=client_credentials" --data-urlencode "client_secret=$secret")
echo $accesstokenresponse
accesstoken=$(echo $accesstokenresponse | grep -oP '(?<="access_token":")[^"]*')
echo ""

# register the access control UI in identity
echo "registering Fabric Authorization Access Control client with Fabric.Identity..."
clientresponse=$(curl -v -k -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-access-control\", \"clientName\": \"Fabric Authorization Access Control\", \"requireConsent\": false, \"allowedGrantTypes\": [\"implicit\"], \"redirectUris\": [\"$authorizationbaseurl/client/oidc-callback.html\", \"$authorizationbaseurl/client/silent.html\"], \"postLogoutRedirectUris\": [ \"$authorizationbaseurl/client/logged-out\"], \"allowOfflineAccess\": false, \"allowAccessTokensViaBrowser\": true, \"allowedCorsOrigins\":[\"$authorizationbaseurl\"], \"requireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"fabric/authorization.internal\",\"fabric/identity.searchusers\", \"fabric/authorization.dos.write\"]}" $identitybaseurl/api/client)
echo $clientresponse
echo ""

# register the access control UI in authorization
echo "registering Fabric Authorization Access Control client with Fabric.Authorization..."
curl -v -k -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"id\": \"fabric-access-control\", \"name\": \"Fabric Authorization Access Control\", \"topLevelSecurableItem\": { \"name\":\"fabric-access-control\"}}" $authorizationbaseurl/clients/
echo ""
