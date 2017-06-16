#!/bin/bash

# Usage:
#     update-roles.sh [ldap host] [ldap port] [ldap base dn]
#     
#     Environment vars also may be set for:
#        - LDAP_HOST  default: localhost'
#        - LDAP_PORT  default: 389
#        - BASE_DN    default: DN=com
#        - REQUIRES_TLS           default: false
#        - REQUIRES_BINDING       default: false
#        - BINDING_DN             default: CN=admin
#        - BINDING_PASSWORD_FILE  default: /run/secrets/ldap 'use default
#        - FABRIC_AUTH_URL        default: https://localhost

################################################################################
# Configuration

if [ $1 ]; then  #Param 1 is LDAP_HOST
  LDAP_HOST=$1
fi

if [ $2 ]; then  #Param 2 is LDAP_PORT
  LDAP_PORT=$2
fi

if [ $3 ]; then  #Param 3 is BASE_DN
  BASE_DN=$3
fi

#Defaults 
if ! [ $LDAP_HOST ]; then
   LDAP_HOST="127.0.0.1"
fi

if ! [ $LDAP_PORT ]; then
   LDAP_PORT=389
fi

if ! [ $BASE_DN ]; then
   BASE_DN="DN=com"
fi

if ! [ $REQUIRES_TLS ]; then
   REQUIRES_TLS=false
fi

#Binding
if ! [ $REQUIRES_BINDING ]; then
   REQUIRES_BINDING=false
fi

if [ $REQUIRES_BINDING == true ]; then

	if ! [ $BINDING_DN ]; then
	   BINDING_DN="CN=admin"
	fi

	if ! [ $BINDING_PASSWORD_FILE ]; then
	   BINDING_PASSWORD_FILE="/run/secrets/ldap.pwd"
	fi

	#REST Endpoint
	if ! [ $FABRIC_AUTH_URL ]; then
	   FABRIC_AUTH_URL="https://localhost"
	fi

fi

################################################################################
# Check for presence of tools

TOKENIZER_COMMAND="awk -F ':' '/cn:/ {print \$2}'"

#ldapsearch
if ! [ -x "$(command -v ldapsearch)" ]; then
    echo FATAL: ldapsearch is not available!
    exit
fi

#grep
if ! [ -x "$(command -v grep)" ]; then
    echo FATAL: grep is not available!
    exit
fi

#awk or cut
if ! [ -x "$(command -v awk)" ]; then
    echo WARN: awk is not available, trying cut.
    if ! [ -x "$(command -v cut)" ]; then
        echo FATAL: cut is not available!
        exit
    else
        TOKENIZER_COMMAND="cut -d':' -f2 | cut -c 1-"
    fi
fi

#curl
if ! [ -x "$(command -v curl)" ]; then
    echo FATAL: curl is not available!
    exit
fi

################################################################################
# Setup params

#Bind params
BINDING_PARAMS=""
if [ $REQUIRES_BINDING == true ]; then
   BINDING_PARAMS="-y $BINDING_PASSWORD_FILE -D '$BINDING_DN'"
fi

#TLS params
TLS_PARAMS=""
if [ $REQUIRES_TLS == true ]; then
   TLS_PARAMS="-ZZ"
fi

#Other params
PARAMS="-x -b '$BASE_DN' -h $LDAP_HOST -p $LDAP_PORT $BINDING_PARAMS $TLS_PARAMS"
QUERY="'(objectClass=group)'"

################################################################################
# Execute query

echo -e "\nLoading groups..."
SEARCH="ldapsearch $PARAMS $QUERY | grep -i 'CN:' | $TOKENIZER_COMMAND"
echo $SEARCH
GROUPS=`eval $SEARCH`

################################################################################
# Call Fabric.Authorization

echo -e "\nAdding groups..."
for group in $GROUPS; do
  eval "curl -i -X POST -d \"GroupName\":\"$group\" $FABRIC_AUTH_URL/groups"
done
