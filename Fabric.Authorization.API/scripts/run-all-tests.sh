#!/bin/bash
couchusername=$1
couchpassword=$2

./run-unit-tests.sh
./run-integration-tests.sh 
./run-functional-tests.sh $couchusername $couchpassword
