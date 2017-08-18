#!/bin/bash

echo "Cloning Git Wiki repo..."
git clone https://$GITWIKIACCESSTOKEN@github.com/HealthCatalyst/Fabric.Authorization.wiki.git

echo "Moving MD files to Fabric.Authorization.wiki..."
mv overview.md API-Reference-Overview.md
mv paths.md API-Reference-Resources.md
mv definitions.md API-Reference-Models.md
mv security.md API-Reference-Security.md

mv *.md Fabric.Authorization.wiki

echo "Changing directory..."
cd Fabric.Authorization.wiki

echo "-----Present directory = $(pwd)-----"

git config user.name "Fabric Authorization System User"
git config user.email "kyle.paul@healthcatalyst.com"
git add *.md
git commit -m 'update API documentation'
git push origin master
