#!/bin/bash

echo "Cloning Git Wiki repo..."
REPO="https://$1@github.com/HealthCatalyst/Fabric.Authorization.wiki.git"
git clone $REPO

echo "Moving MD files to Fabric.Authorization.wiki..."
mv overview.md API-Reference-Overview.md
mv paths.md API-Reference-Resources.md
mv definitions.md API-Reference-Models.md
mv security.md API-Reference-Security.md

mv *.md Fabric.Authorization.wiki

echo "Changing directory..."
cd Fabric.Authorization.wiki

echo "-----Present directory = $(pwd)-----"

git config user.name "vsts build"
git config user.email "kyle.paul@healthcatalyst.com"
git add *.md

echo "committing files"
git commit -m 'update API documentation'
echo "pushing files to github"
git push origin master