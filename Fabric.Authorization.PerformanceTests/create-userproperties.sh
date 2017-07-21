#!/bin/bash

authserver=$1
authport=$2
identityserver=$3
identityport=$4
bobpassword=$5
alicepassword=$6

cat > user.properties << EOF
identityserver=$identityserver
identityport=$identityport
authserver=$authserver
authport=$authport
bobpassword=$bobpassword
alicepassword=$alicepassword
EOF
