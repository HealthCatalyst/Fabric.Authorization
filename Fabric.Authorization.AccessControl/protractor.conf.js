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
    }
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
  baseUrl: 'https:///mvidalweb2016.hqcatalyst.local/Authorization',
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

    const username = encodeURIComponent(browser.params.login.username);
    const password = encodeURIComponent(browser.params.login.password);
    const discoveryUrl = `https://${username}:${password}@mvidalweb2016.hqcatalyst.local/DiscoveryService/v1`

    console.log('Logging into Discovery');

    return browser.driver.get(discoveryUrl)
      .then(() => {
        console.log('Logging into Identity');
        return browser.driver.get('https://mvidalweb2016.hqcatalyst.local/identity/account/ExternalLogin?provider=Negotiate');
      })
      .then(() => {
        console.log('Logging into Access Control');
        return browser.driver.get('https://mvidalweb2016.hqcatalyst.local/Authorization');
      })
      .then(() => console.log('Fully logged in'))
      .then(() => browser.driver.sleep(2000));  // wait for login process
  }
};
