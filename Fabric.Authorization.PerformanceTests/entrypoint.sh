#!/bin/bash

./create-userproperties.sh $AUTH_SERVER $AUTH_PORT $IDENTITY_SERVER $IDENTITY_PORT $BOB_PASSWORD $ALICE_PASSWORD
mv user.properties /jmeter/apache-jmeter-3.2/bin/user.properties
mkdir results
mkdir results/output
/jmeter/apache-jmeter-3.2/bin/jmeter -n -t /Fabric.Authorization.Perf.jmx -l /results/results.txt -e -o /results/output
cd /apdexcalc
dotnet /apdexcalc/Fabric.Authorization.ApdexCalculator.dll /results/results.txt
