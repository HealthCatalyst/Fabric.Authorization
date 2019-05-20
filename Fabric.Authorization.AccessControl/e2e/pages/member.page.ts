import { by, element, browser, protractor } from 'protractor';
import { MemberListPage } from './member-list.page';

export class MemberPage {
    DataMartAdmin = 'DataMart Admin';
    PublicEntityReader = 'Public Entity Reader';

    getSaveButton() { return element(by.buttonText('Save')); }
    getSearchBox() { return element(by.className('hc-input')); }
    getRolesTable() { return element(by.className('hc-table')); }
    getCheckedCheckBoxLocator() { return by.className('hc-checkbox-checked'); }

    searchPrincipal(searchString: string) {
        this.getSearchBox().sendKeys(searchString);
    }

    async save() {
        const until = protractor.ExpectedConditions;
        const saveButton = this.getSaveButton();
        return browser.wait(until.elementToBeClickable(saveButton), browser.allScriptsTimeout, 'save button was not enabled')
            .then(() => saveButton.click());
    }

    getRoleRow(roleToSelect: string) {
        return this.getRolesTable().element(by.css('tbody')).all(by.css('tr')).filter((elem) => {
            return elem.getText().then((text) => {
                return text.includes(roleToSelect);
            });
        }).first();
    }

    getIdpssPrincipalResult(principalType: string) {
        return element(by.className('member-list')).all(by.css('li')).filter((elem) => {
            return elem.getText().then((text) => {
                return text.includes(`(${principalType})`); // select user list element
            });
        }).first();
    }

    async searchForAndSelectPrincipal(searchString: string, principalType: string) {
        const until = protractor.ExpectedConditions;
        const saveButton = this.getSaveButton();
        await expect(saveButton.isEnabled()).toBe(false, 'save button was not disabled before selecting a user');

        // search user
        this.searchPrincipal(searchString);

        // select result
        const principalListElement = this.getIdpssPrincipalResult(principalType);
        await browser.wait(until.visibilityOf(principalListElement), browser.allScriptsTimeout, 'IdPSS search results were not found');

        // overlay contains the actual clickable checkbox
        const userSelectionCheckBox = principalListElement.element(by.className('hc-checkbox-overlay'));
        return userSelectionCheckBox.click();
    }

      async selectRoleAndSave(roleToSelect: string) {
        const roleRow = this.getRoleRow(roleToSelect);
        const firstRoleCheckBox = roleRow.element(by.className('hc-checkbox-overlay'));
        return firstRoleCheckBox.click()
            .then(() => this.save())
            .then(() => this.waitUntilMainPageLoaded()); // necessary so test does not end before saving roles
    }

    private async waitUntilMainPageLoaded() {
        const mainPage = new MemberListPage();
        const until = protractor.ExpectedConditions;
        const addButton = mainPage.getAddButton();
        return browser.wait(until.presenceOf(addButton), browser.allScriptsTimeout, 'Main page was not loaded');
    }
}
