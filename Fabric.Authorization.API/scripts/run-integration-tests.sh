#!/bin/bash
couchusername=$1
couchpassword=$2

docker stop authorization.integration.couchdb
docker rm authorization.integration.couchdb
docker volume rm authorization-int-db-data
docker run -d --name authorization.integration.couchdb -p 5986:5984 -e COUCHDB_USER=$couchusername -e COUCHDB_PASSWORD=$couchpassword -v authorization-int-db-data://opt/couch/data healthcatalyst/fabric.docker.couchdb

export COUCHDBSETTINGS__USERNAME=$couchusername
export COUCHDBSETTINGS__PASSWORD=$couchpassword
export COUCHDBSETTINGS__SERVER=http://localhost:5986

sleep 3
"/C/Program Files (x86)/Microsoft Visual Studio/2017/Professional/MSBuild/15.0/Bin/MSBuild.exe" ../../Fabric.Authorization.SqlServer/Fabric.Authorization.SqlServer.sqlproj
cp ../../Fabric.Authorization.SqlServer/bin/Debug/Fabric.Authorization.SqlServer_Create.sql ../../Fabric.Authorization.IntegrationTests/bin/Debug/netcoreapp1.1/Fabric.Authorization.SqlServer_Create.sql

dotnet test ../../Fabric.Authorization.IntegrationTests/Fabric.Authorization.IntegrationTests.csproj

docker stop authorization.integration.couchdb
docker rm authorization.integration.couchdb
docker volume rm authorization-int-db-data
