#!/bin/bash

################################################################################
# Configuration

FREQUENCY="0 0,8,16 * * *" #Every 8 hours
SCRIPT_LOCATION="/usr/local/bin"

################################################################################
# Check for presence of tools

#crontab
if ! [ -x "$(command -v crontab)" ]; then
    echo FATAL: crontab is not available!
    exit
fi

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
    fi
fi

#curl
if ! [ -x "$(command -v curl)" ]; then
    echo FATAL: curl is not available!
    exit
fi

################################################################################
# Setup crontab

eval "(crontab -l 2>/dev/null; echo '$FREQUENCY $SCRIPT_LOCATION/update-roles.sh') | crontab -" 
