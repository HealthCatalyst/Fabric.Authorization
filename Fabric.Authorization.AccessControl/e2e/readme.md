# Pre-requisites
## Install Protractor
1. Protractor must be installed.
2. Selenium web driver must be installed.
See the protractor [page](https://www.protractortest.org/#/) for install details.

## Configure config file
The config file `protractor.conf.js` is located under the AccessControl root folder.
1. Update the serverRoot field to point to the server name AccessControl is being hosted on.
    * Can also be set by environment variable: E2E_SERVERROOT
2. Update the baseUrl field to point to a valid AccessControl url being tested.
    * Can also be set by environment variable: E2E_BASEURL
3. Update the login details.
    * The config file has a login section which takes the values from an expected file named `account.config.json` under the e2e folder. Create the account.config.json which two fields: `adminWindowsUserName`, `adminWindowsPassword`. The account specified in the `account.config.json` should be an DosAdmin in the AccessControl instance being tested.
    * Can also be set by environment variables: E2E_USERNAME, E2E_PASSWORD
* Any environment variables will overwrite protractor.config.js values


# Running Functional Tests
To run all tests defined in the spec portion of the config file:
>`protractor protractor.conf.js`

To run a specific suite of tests:
>`protractor protractor.conf.js --suite=<suitename>`

To run a specific spec file:
>`protractor protractor.conf.js --specs=<path-to-spec-file>`