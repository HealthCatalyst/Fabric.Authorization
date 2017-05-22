#!/bin/bash
read -n 1 -p 'Are you sure you want to publish to dockerhub?'
docker stop fabric.authorization
docker rm fabric.authorization

dotnet publish --configuration Release --output obj/Docker/publish
docker build -t healthcatalyst/fabric.authorization .
docker push healthcatalyst/fabric.authorization

echo Press any key to exit
read -n 1
