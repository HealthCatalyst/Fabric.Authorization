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
    const until = protractor.ExpectedConditions;
    const checkBoxContainer = memberPage.getRoleRow(memberPage.DataMartAdmin)
      .element(by.className('hc-checkbox-checked'));
    browser.wait(until.visibilityOf(checkBoxContainer), 3000, 'Selected role was not selected on re-search');  // fails test if not found

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
    const until = protractor.ExpectedConditions;
    const checkBoxContainer = memberPage.getRoleRow(memberPage.PublicEntityReader)
      .element(by.className('hc-checkbox-checked'));
    browser.wait(until.visibilityOf(checkBoxContainer), 3000, 'Selected role was not selected on re-search');  // fails test if not found

    // remove role (reset) user
    memberPage.selectRoleAndSave(memberPage.PublicEntityReader);
  });
});
