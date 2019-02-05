import { by, element, browser, protractor } from 'protractor';
import { MemberListPage } from './member-list.page';

export class MemberPage {
    DataMartAdmin = 'DataMart Admin';
    PublicEntityReader = 'Public Entity Reader';

    getSaveButton() { return element(by.buttonText('Save')); }
    getSearchBox() { return element(by.className('hc-input')); }
    getRolesTable() { return element(by.className('hc-table')); }

    searchPrincipal(searchString: string) {
        this.getSearchBox().sendKeys(searchString);
    }

    save() {
        const saveButton = this.getSaveButton();
        expect(saveButton.isEnabled()).toBe(true, 'save button was not enabled');
        saveButton.click();
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

    searchForAndSelectPrincipal(searchString: string, principalType: string) {
        const until = protractor.ExpectedConditions;
        const saveButton = this.getSaveButton();
        expect(saveButton.isEnabled()).toBe(false, 'save button was not disabled before selecting a user');

        // search user
        this.searchPrincipal(searchString);

        // select result
        const principalListElement = this.getIdpssPrincipalResult(principalType);
        browser.wait(until.visibilityOf(principalListElement), 3000, 'IdPSS search results were not found');

        // overlay contains the actual clickable checkbox
        const userSelectionCheckBox = principalListElement.element(by.className('hc-checkbox-overlay'));
        userSelectionCheckBox.click();
    }

    selectRoleAndSave(roleToSelect: string) {
        const roleRow = this.getRoleRow(roleToSelect);
        const firstRoleCheckBox = roleRow.element(by.className('hc-checkbox-overlay'));
        firstRoleCheckBox.click();

        this.save();
        this.waitUntilMainPageLoaded(); // necessary so test does not end before saving roles
    }

    private waitUntilMainPageLoaded() {
        const mainPage = new MemberListPage();
        const until = protractor.ExpectedConditions;
        const addButton = mainPage.getAddButton();
        browser.wait(until.presenceOf(addButton), 3000, 'Main page was not loaded');
    }
}
