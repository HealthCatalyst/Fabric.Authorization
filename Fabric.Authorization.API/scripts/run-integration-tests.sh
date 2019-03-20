#!/bin/bash

"/C/Program Files (x86)/Microsoft Visual Studio/2017/Professional/MSBuild/15.0/Bin/MSBuild.exe" ../../Fabric.Authorization.SqlServer/Fabric.Authorization.SqlServer.sqlproj
cp ../../Fabric.Authorization.SqlServer/bin/Debug/Fabric.Authorization.SqlServer_Create.sql ../../Fabric.Authorization.IntegrationTests/bin/Debug/netcoreapp2.2/Fabric.Authorization.SqlServer_Create.sql

dotnet test ../../Fabric.Authorization.IntegrationTests/Fabric.Authorization.IntegrationTests.csproj
