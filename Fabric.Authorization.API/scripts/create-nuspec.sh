#!/bin/bash		
version=$1		
artifactPath=$2		
echo "current directory: ${PWD}"		
echo "version set to: $version"		
echo "path to artifacts: $artifactPath"		
cat > Fabric.Authorization.API.nuspec << EOF		
<?xml version="1.0" encoding="utf-8"?>		
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">		
    <metadata> 		
            <!-- Required elements-->		
            <id>Fabric.Authorization.InstallPackage</id>		
            <version>$version</version>		
            <authors>Health Catalyst</authors>		
            <owners>Health Catalyst</owners>		
            <requireLicenseAcceptance>false</requireLicenseAcceptance>		
            <description>Install package for Fabric.Authorization</description>		
    </metadata>		
    <files>		
        <file src="$artifactPath/Fabric.Authorization.API.zip" target="build" />
        <file src="$artifactPath/Install-Authorization-Windows.ps1" target="build" />
	<file src="$artifactPath/Fabric-Install-Utilities.psm1" target="build" />
	<file src="$artifactPath/install.config" target="build" />
    </files>		
</package>		
EOF
