import { browser, protractor, element, by } from 'protractor';

export class AccessControlPage {
    getMainPage() {
        browser.waitForAngularEnabled(false)
            .then(() => browser.get(browser.baseUrl));

        browser.driver.sleep(2000);
    }

    searchForAndSelectUser(searchString: string, principalType: string) {
        // click add group/user
        const until = protractor.ExpectedConditions;
        const addButton = element(by.buttonText('Add'));
        browser.wait(until.presenceOf(addButton), 3000, 'Add button on main page was not found');  // wait until loaded...
        addButton.click();
        const userGroupLink = element(by.linkText('Directory Group or User'));
        browser.wait(until.presenceOf(userGroupLink), 3000, 'Link to member page was not found');
        userGroupLink.click();

        const saveButton = element(by.buttonText('Save'));
        expect(saveButton.isEnabled()).toBe(false, 'save button was not disabled before selecting a user');

        // search user
        element(by.className('hc-input')).sendKeys(searchString);

        // select result
        const principalListElement = element(by.className('member-list')).all(by.css('li')).filter((elem) => {
        return elem.getText().then((text) => {
            return text.includes(`(${principalType})`); // select user list element
        });
        }).first();
        browser.wait(until.visibilityOf(principalListElement), 3000, 'IdPSS search results were not found');

        // overlay contains the actual clickable checkbox
        const userSelectionCheckBox = principalListElement.element(by.className('hc-checkbox-overlay'));
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
        this.waitUntilMainPageLoaded();
    }

    private waitUntilMainPageLoaded() {
        const until = protractor.ExpectedConditions;
        const addButton = element(by.buttonText('Add'));
        browser.wait(until.presenceOf(addButton), 3000, 'Main page was not loaded');
    }
}
