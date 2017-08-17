#!/bin/bash

AUTHZIP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $AUTHORIZATIONCONTAINERNAME)
echo "Authorization Container running on $AUTHZIP"
echo "##vso[task.setvariable variable=FabricAuthorizationBaseUrl;]http://$AUTHZIP:5004"
echo "Authorization Server URL (FABRICAUTHORIZATIONBASEURL): $FABRICAUTHORIZATIONBASEURL"
echo "Authorization Server URL (FabricAuthorizationBaseUrl): $FabricAuthorizationBaseUrl"