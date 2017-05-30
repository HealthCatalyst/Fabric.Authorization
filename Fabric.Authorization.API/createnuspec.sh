#!/bin/bash
version=$1
zippedPackagePath=$2
echo "current directory: ${PWD}"
echo "version set to: $version"
echo "path to zipped package: $zippedPackagePath"
cat > Fabric.Authorization.API.nuspec << EOF
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
    <metadata> 
        <!-- Required elements-->
        <id>Fabric.Authorization.WebDeployPackage</id>
        <version>$version</version>
        <authors>Health Catalyst</authors>
        <owners>Health Catalyst</owners>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>Web Deploy package for Fabric.Authorization</description>
        <!-- Optional elements -->
        <!-- ... -->
    </metadata>
    <files>
        <file src="$zippedPackagePath" target="build" />
    </files>
</package>
EOF
