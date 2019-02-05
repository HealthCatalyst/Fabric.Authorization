import { browser, protractor, element, by } from 'protractor';
import { tellNgZoneItHasNoPendingMacroTasks } from '../hacks';
import { MemberPage } from './member.page';

export class MemberListPage {
    async getMemberListPage() {
        browser.get(browser.baseUrl);
        tellNgZoneItHasNoPendingMacroTasks();
    }

    getAddButton() { return element(by.buttonText('Add')); }

    navigateToMemberPage() {
        const until = protractor.ExpectedConditions;
        const addButton = this.getAddButton();
        browser.wait(until.presenceOf(addButton), 3000, 'Add button on main page was not found');  // wait until loaded...
        addButton.click();

        const userGroupLink = element(by.linkText('Directory Group or User'));
        browser.wait(until.presenceOf(userGroupLink), 3000, 'Link to member page was not found');
        userGroupLink.click();
        return new MemberPage();
    }
}
