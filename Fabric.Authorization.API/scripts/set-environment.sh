#!/bin/bash

IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $COUCHDBCONTAINERNAME)
echo "CouchDb Container running on $IP"
echo "##vso[task.setvariable variable=CouchDbSettings__Server;]http://$IP:5984"
echo "CouchDb Server URL: $COUCHDBSETTINGS_SERVER"

