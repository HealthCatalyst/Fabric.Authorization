#!/bin/bash

installerSecretString=$1
authClientSecretString=$2
stagingDirectory=$3

json="{\"installerSecret\":\""$installerSecretString"\", \"authClientSecret\":\""$authClientSecretString"\"}"  

echo -e $json
echo $stagingDirectory
echo $json > $stagingDirectory"/variables.txt"