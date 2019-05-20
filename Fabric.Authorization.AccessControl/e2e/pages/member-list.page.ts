import { browser, protractor, element, by } from 'protractor';
import { tellNgZoneItHasNoPendingMacroTasks } from '../hacks';
import { MemberPage } from './member.page';

export class MemberListPage {
    async getMemberListPage() {
        await browser.get(browser.baseUrl);
        await tellNgZoneItHasNoPendingMacroTasks();
    }

    getAddButton() { return element(by.buttonText('Add')); }

    async navigateToMemberPage() {
        const until = protractor.ExpectedConditions;
        const addButton = this.getAddButton();
        await browser
            .wait(until.presenceOf(addButton), browser.allScriptsTimeout, 'Add button on main page was not found');  // wait until loaded...
        await addButton.click();

        const userGroupLink = element(by.linkText('Directory Group or User'));
        await browser
            .wait(until.presenceOf(userGroupLink), browser.allScriptsTimeout, 'Link to member page was not found');
        await userGroupLink.click();
        return new MemberPage();
    }
}
