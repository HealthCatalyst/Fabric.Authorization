import { protractor, element, browser, by } from 'protractor';
import { MemberListPage } from '../pages/member-list.page';
import { MemberPage } from './../pages/member.page';

describe('member-list page', () => {
  let mainPage: MemberListPage;

  beforeEach(() => {
    mainPage = new MemberListPage();
  });

  it('should search and add a user with roles, and then remove the roles', () => {
    mainPage.getMemberListPage();

    const memberPage: MemberPage = mainPage.navigateToMemberPage();
    memberPage.searchForAndSelectPrincipal('functional test user', 'user');
    memberPage.selectRoleAndSave(memberPage.DataMartAdmin);

    // re-search existing user (with role added)
    mainPage.navigateToMemberPage();
    memberPage.searchForAndSelectPrincipal('functional test user', 'user');

    // verify user has roles from above
    const roleRow = memberPage.getRoleRow(memberPage.DataMartAdmin);
    expect(roleRow.isElementPresent(memberPage.getCheckedCheckBoxLocator()))
      .toBe(true, 'Role was not select on re-search');

    // remove role (reset) user
    memberPage.selectRoleAndSave(memberPage.DataMartAdmin);
  });

  it('should search and add an ad group with roles, and then remove the roles', () => {
    mainPage.getMemberListPage();

    const memberPage = mainPage.navigateToMemberPage();
    memberPage.searchForAndSelectPrincipal('functional test group', 'group');
    memberPage.selectRoleAndSave(memberPage.PublicEntityReader);

    // re-search existing user (with role added)
    mainPage.navigateToMemberPage();
    memberPage.searchForAndSelectPrincipal('functional test group', 'group');

    // verify user has roles from above
    const roleRow = memberPage.getRoleRow(memberPage.PublicEntityReader);
    expect(roleRow.isElementPresent(memberPage.getCheckedCheckBoxLocator()))
      .toBe(true, 'Role was not select on re-search');

    // remove role (reset) user
    memberPage.selectRoleAndSave(memberPage.PublicEntityReader);
  });
});
