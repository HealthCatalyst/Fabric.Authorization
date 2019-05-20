import { protractor,  browser } from 'protractor';
import { MemberListPage } from '../pages/member-list.page';
import { MemberPage } from './../pages/member.page';

describe('member page', () => {
  let mainPage: MemberListPage;

  beforeEach(() => {
    mainPage = new MemberListPage();
  });

  it('should search and add a user with roles, and then remove the roles', async () => {
    const until = protractor.ExpectedConditions;
    await mainPage.getMemberListPage();

    const memberPage: MemberPage = await mainPage.navigateToMemberPage();
    await memberPage.searchForAndSelectPrincipal('functional test user', 'user');
    await memberPage.selectRoleAndSave(memberPage.DataMartAdmin);

    // re-search existing user (with role added)
    await mainPage.navigateToMemberPage();
    await memberPage.searchForAndSelectPrincipal('functional test user', 'user');

    // verify user has roles from above
    const roleRow = memberPage.getRoleRow(memberPage.DataMartAdmin);
    const checkbox = roleRow.element(memberPage.getCheckedCheckBoxLocator());
    await browser.wait(until.visibilityOf(checkbox), browser.allScriptsTimeout, 'Check box was not found..');

    // remove role (reset) user
    await memberPage.selectRoleAndSave(memberPage.DataMartAdmin);
  });

  it('should search and add an ad group with roles, and then remove the roles', async () => {
    const until = protractor.ExpectedConditions;
    await mainPage.getMemberListPage();

    const memberPage = await mainPage.navigateToMemberPage();
    await memberPage.searchForAndSelectPrincipal('functional test group', 'group');
    await memberPage.selectRoleAndSave(memberPage.PublicEntityReader);

    // re-search existing user (with role added)
    await mainPage.navigateToMemberPage();
    await memberPage.searchForAndSelectPrincipal('functional test group', 'group');

    // verify user has roles from above
    const roleRow = memberPage.getRoleRow(memberPage.PublicEntityReader);
    const checkbox = roleRow.element(memberPage.getCheckedCheckBoxLocator());
    await browser.wait(until.visibilityOf(checkbox), browser.allScriptsTimeout, 'Check box was not found..');

    // remove role (reset) user
    await memberPage.selectRoleAndSave(memberPage.PublicEntityReader);
  });
});
