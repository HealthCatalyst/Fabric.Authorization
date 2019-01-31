import { browser, element, by, protractor } from 'protractor';

describe('adding a user', () => {
    it('should search and add a user with roles, and then remove the roles', () => {
      // can be own thing..
      browser.waitForAngularEnabled(false)  // a redirect occurs for login..
        .then(() => browser.get('https://mvidalweb2016.hqcatalyst.local/Authorization/client/access-control'));

      browser.driver.sleep(2000); // see if i can wait for angular above and make that work..
      // click add group/user
      const until = protractor.ExpectedConditions;
      const addButton = element(by.buttonText('Add'));
      browser.wait(until.presenceOf(addButton), 5000);  // wait until loaded...
      addButton.click();
      const userGroupLink = element(by.linkText('Directory Group or User'));
      browser.wait(until.presenceOf(userGroupLink), 5000);
      userGroupLink.click();

      const saveButton = element(by.buttonText('Save'));
      expect(saveButton.isEnabled()).toBe(false, 'save button was not disabled before selecting a user');

      // search user
      const userToSearch = 'functional test user';
      element(by.className('hc-input')).sendKeys(userToSearch);
      browser.driver.sleep(3000); // manual sleep for idpss search results..

      // select result
      let userListElement = element(by.className('member-list')).all(by.css('li')).filter((elem) => {
        return elem.getText().then((text) => {
          return text.includes('(user)'); // select user list element
        });
      }).first();
      let userSelectionCheckBox = userListElement.element(by.className('hc-checkbox-overlay')); // overlay contains actual checkbox..
      browser.wait(until.elementToBeClickable(userSelectionCheckBox));
      userSelectionCheckBox.click();

      // add role(s)
      let rolesTable = element(by.className('hc-table'));
      let firstRoleCheckBox = rolesTable.element(by.css('tbody')).all(by.className('hc-checkbox-overlay')).first();
      browser.wait(until.elementToBeClickable(firstRoleCheckBox));
      firstRoleCheckBox.click();

      expect(saveButton.isEnabled()).toBe(true, 'save button was not enabled');
      saveButton.click();

      // do the same as above (pull out into page later..)
      browser.wait(until.presenceOf(addButton), 5000);
      addButton.click();
      browser.wait(until.presenceOf(userGroupLink), 5000);
      userGroupLink.click();

      // search, same as above..
      element(by.className('hc-input')).sendKeys(userToSearch);
      browser.driver.sleep(3000); // manual sleep for idpss search results..

      // select result, same as above..
      userListElement = element(by.className('member-list')).all(by.css('li')).filter((elem) => {
        return elem.getText().then((text) => {
          return text.includes('(user)'); // select user list element
        });
      }).first();
      userSelectionCheckBox = userListElement.element(by.className('hc-checkbox-overlay'));
      browser.wait(until.elementToBeClickable(userSelectionCheckBox));
      userSelectionCheckBox.click();

      rolesTable = element(by.className('hc-table'));
      firstRoleCheckBox = rolesTable.element(by.css('tbody')).all(by.className('hc-checkbox-overlay')).first(); // select first checkbox

      const checkBoxContainer = rolesTable.element(by.css('tbody')).all(by.className('hc-checkbox-checked')).first();
      browser.wait(until.visibilityOf(checkBoxContainer), 5000);  // fails test if not found

      // remove role and save
      firstRoleCheckBox.click();  // unclick
      expect(saveButton.isEnabled()).toBe(true, 'save button was not enabled');
      saveButton.click();
    });
  });
