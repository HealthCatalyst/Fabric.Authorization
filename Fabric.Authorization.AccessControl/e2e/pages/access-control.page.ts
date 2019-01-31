import { browser, protractor, element, by } from 'protractor';

export class AccessControlPage {
    getMainPage() {
        browser.waitForAngularEnabled(false)  // a redirect occurs for login?
        .then(() => browser.get(browser.baseUrl));

        browser.driver.sleep(2000); // see if i can wait for angular above and make that work..
    }

    searchForAndSelectUser(userToSearch: string) {
        // click add group/user
        const until = protractor.ExpectedConditions;
        const addButton = element(by.buttonText('Add'));
        browser.wait(until.presenceOf(addButton), 5000);  // wait until loaded...
        addButton.click();
        const userGroupLink = element(by.linkText('Directory Group or User'));
        browser.wait(until.presenceOf(userGroupLink), 3000);
        userGroupLink.click();

        const saveButton = element(by.buttonText('Save'));
        expect(saveButton.isEnabled()).toBe(false, 'save button was not disabled before selecting a user');

        // search user
        element(by.className('hc-input')).sendKeys(userToSearch);
        browser.driver.sleep(3000); // manual sleep for idpss search results..

        // select result
        const userListElement = element(by.className('member-list')).all(by.css('li')).filter((elem) => {
        return elem.getText().then((text) => {
            return text.includes('(user)'); // select user list element
        });
        }).first();
        const userSelectionCheckBox = userListElement.element(by.className('hc-checkbox-overlay')); // overlay contains actual checkbox..
        browser.wait(until.elementToBeClickable(userSelectionCheckBox));
        userSelectionCheckBox.click();
    }

    // TODO: make this search for a specific role
    selectFirstRoleAndSave() {
        // add role(s)
        const until = protractor.ExpectedConditions;
        const saveButton = element(by.buttonText('Save'));
        const rolesTable = element(by.className('hc-table'));
        const firstRoleCheckBox = rolesTable.element(by.css('tbody')).all(by.className('hc-checkbox-overlay')).first();
        browser.wait(until.elementToBeClickable(firstRoleCheckBox));
        firstRoleCheckBox.click();

        expect(saveButton.isEnabled()).toBe(true, 'save button was not enabled');
        saveButton.click();
    }
}
