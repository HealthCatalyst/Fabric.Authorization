// Protractor configuration file, see link for more information
// https://github.com/angular/protractor/blob/master/lib/config.ts

const { SpecReporter } = require('jasmine-spec-reporter');

let credentials;
credentials = require('./e2e/account.config.json');

exports.config = {
  allScriptsTimeout: 11000,
  getPageTimeout: 11000,
  seleniumAddress: 'http://localhost:4444/wd/hub',
  params: {
    login: {
      username: credentials.adminWindowsUserName,
      password: credentials.adminWindowsPassword
    },
    serverRoot: 'localhost',
  },
  suites: {
    admin: './e2e/admin-tests/*.spec.ts'
  },
  specs: [
    './e2e/**/*.spec.ts'
  ],
  capabilities: {
    'browserName': 'chrome'
  },
  directConnect: true,
  baseUrl: 'http://localhost/Authorization',
  framework: 'jasmine',
  jasmineNodeOpts: {
    showColors: true,
    defaultTimeoutInterval: 30000,
    print: function() {}
  },
  onPrepare() {
    require('ts-node').register({
      project: 'e2e/tsconfig.e2e.json'
    });
    jasmine.getEnv().addReporter(new SpecReporter({ spec: { displayStacktrace: true } }));
    browser.driver.manage().window().maximize();

    // take environment variables over config values
    const username = process.env.E2E_USERNAME || browser.params.login.username;
    const password =  process.env.E2E_PASSWORD || browser.params.login.password;
    const root = process.env.E2E_SERVERROOT || browser.params.serverRoot;
    browser.baseUrl = process.env.E2E_BASEURL || browser.baseUrl;

    const discoveryUrl = `https://${encodeURIComponent(username)}:${encodeURIComponent(password)}@${root}/DiscoveryService/v1`

    console.log('Logging into Discovery');

    return browser.driver.get(discoveryUrl)
      .then(() => {
        console.log('Logging into Identity');
        return browser.driver.get(`https://${root}/identity/account/ExternalLogin?provider=Negotiate`);
      })
      .then(() => {
        console.log('Logging into Access Control');
        return browser.driver.get(browser.baseUrl);
      })
      .then(() => console.log('Fully logged in'))
      .then(() => browser.driver.sleep(2000));  // wait for login process
  }
};
