#!/bin/bash

installerSecretString=$1
authClientSecretString=$2
destinationFile=$3

json="{\"installerSecret\":\""$installerSecretString"\", \"authClientSecret\":\""$authClientSecretString"\"}"  

echo -e $json
echo $destinationFile
echo $json > $destinationFile