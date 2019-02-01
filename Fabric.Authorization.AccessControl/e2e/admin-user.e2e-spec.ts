import { protractor, element, browser, by } from 'protractor';
import { AccessControlPage } from './pages/access-control.page';

describe('adding a user', () => {
  let page: AccessControlPage;

  beforeEach(() => {
    page = new AccessControlPage();
  });

  it('should search and add a user with roles, and then remove the roles', () => {
    page.getMainPage();

    page.searchForAndSelectUser('functional test user', 'user');

    page.selectFirstRoleAndSave();

    // re-search existing user (with role added)
    page.searchForAndSelectUser('functional test user', 'user');

    // verify user has roles from above
    const until = protractor.ExpectedConditions;
    const rolesTable = element(by.className('hc-table'));
    const checkBoxContainer = rolesTable.element(by.css('tbody')).all(by.className('hc-checkbox-checked')).first();
    browser.wait(until.visibilityOf(checkBoxContainer), 3000, 'Selected role was not found on re-search');  // fails test if not found

    // remove role (reset) user
    page.selectFirstRoleAndSave();
  });
});
