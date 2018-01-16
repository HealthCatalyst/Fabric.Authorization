#!/bin/bash
definitionId=$1
queueBuildResponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $SYSTEM_ACCESSTOKEN" -d "{ \"definition\": { \"id\": $definitionId } }" https://healthcatalyst.visualstudio.com/DefaultCollection/Fabric/_apis/build/builds?api-version=2.0)
echo $queueBuildResponse

buildId=$(echo ${queueBuildResponse} | jq .id)
echo "BuildId: $buildId"

status="\"notStarted\""
completedStatus="\"completed\""
while [ "$status" != "$completedStatus" ]; do
    buildStatusResponse=$(curl -H "Authorization: Bearer $SYSTEM_ACCESSTOKEN" "https://healthcatalyst.visualstudio.com/DefaultCollection/Fabric/_apis/build/builds/$buildId?api-version=2.0")
    status=$(echo ${buildStatusResponse} | jq .status)
    result=$(echo ${buildStatusResponse} | jq .result)
    sleep 10
done

echo "Build Status: $status, Build Result: $result"
expectedResult="\"succeeded\""
if [ "$result" != "$expectedResult" ]; then
    echo "Remote build failed."
    exit 1
fi

buildArtifactResponse=$(curl -H "Authorization: Bearer $SYSTEM_ACCESSTOKEN" "https://healthcatalyst.visualstudio.com/DefaultCollection/Fabric/_apis/build/builds/$buildId/artifacts?api-version=2.0")
artifactName=$(echo ${buildArtifactResponse} | jq .value[0].name)
if [ "$artifactName" != "\"drop\"" ]; then
    echo "Drop file not present or not in expected format."
    exit 1
fi

downloadUrl=$(echo ${buildArtifactResponse} | jq .value[0].resource.downloadUrl)
downloadUrl="${downloadUrl%\"}"
downloadUrl="${downloadUrl#\"}"
echo "Downloading build artifact from $downloadUrl."
curl -H "Authorization: Bearer $SYSTEM_ACCESSTOKEN" "$downloadUrl" > dacpac.zip


