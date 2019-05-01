import { Subject ,  Observable, of, throwError as observableThrowError } from 'rxjs';
import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { ToastrModule } from 'ngx-toastr';

import { CustomGroupComponent } from './custom-group.component';
import { ServicesMockModule } from '../services.mock.module';
import { FormsModule } from '@angular/forms';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthRoleServiceMock } from '../../../services/fabric-auth-role.service.mock';
import {
  FabricAuthGroupServiceMock,
  mockUsersResponse,
  mockGroupsResponse,
  mockGroupResponse } from '../../../services/fabric-auth-group.service.mock';
import {
  mockRolesResponse,
  FabricAuthUserServiceMock,
  mockUserPermissionResponse,
  mockUserResponse } from '../../../services/fabric-auth-user.service.mock';
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule,
  ProgressIndicatorsModule
} from '@healthcatalyst/cashmere';
import { InputDirective } from '../input.directive';
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';
import { IdPSearchResult } from '../../../models/idpSearchResult.model';
import { CurrentUserServiceMock, mockCurrentUserPermissions } from '../../../services/current-user.service.mock';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { CurrentUserService } from '../../../services/current-user.service';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { AlertServiceMock } from '../../../services/global/alert.service.mock';
import { FabricAuthEdwadminServiceMock } from '../../../services/fabric-auth-edwadmin.service.mock';
import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { FabricAuthEdwAdminService } from '../../../services/fabric-auth-edwadmin.service';
import { AlertService } from '../../../services/global/alert.service';
import { Router } from '@angular/router';
import { MockAuthService } from '../../../services/global/auth.service.mock';

describe('CustomGroupComponent', () => {
  let component: CustomGroupComponent;
  let fixture: ComponentFixture<CustomGroupComponent>;
  let IdpSearchResultsSubject: Subject<IdPSearchResult>;
  let searchService: FabricExternalIdpSearchServiceMock;
  let edwAdminService: FabricAuthEdwadminServiceMock;
  let alertService: AlertServiceMock;
  let groupService: FabricAuthGroupServiceMock;
  let router: Router;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupComponent, InputDirective],
        imports: [FormsModule,
          ServicesMockModule,
          ButtonModule,
          IconModule,
          PopoverModule,
          InputModule,
          LabelModule,
          CheckboxModule,
          ProgressIndicatorsModule,
          ToastrModule.forRoot()],
        providers: [
          {
            provide: 'IAuthService',
            useClass: MockAuthService
          }
         ]
      }).compileComponents();
    })
  );

  beforeEach(inject([
    FabricAuthGroupService,
    FabricAuthRoleService,
    FabricExternalIdpSearchService,
    FabricAuthUserService,
    CurrentUserService,
    FabricAuthEdwAdminService,
    AlertService,
    Router],
    (groupServiceMock: FabricAuthGroupServiceMock,
      roleServiceMock: FabricAuthRoleServiceMock,
      searchServiceMock: FabricExternalIdpSearchServiceMock,
      userServiceMock: FabricAuthUserServiceMock,
      currentUserServiceMock: CurrentUserServiceMock,
      edwAdminServiceMock: FabricAuthEdwadminServiceMock,
      alertServiceMock: AlertServiceMock,
      routerMock: Router) => {

      roleServiceMock.getRolesBySecurableItemAndGrain.and.returnValue(of(mockRolesResponse));
      searchService = searchServiceMock;

      IdpSearchResultsSubject = new Subject<IdPSearchResult>();
      searchService.search.and.callFake((searchText: Observable<string>, type: string) => {
        return IdpSearchResultsSubject;
      });

      edwAdminService = edwAdminServiceMock;
      alertService = alertServiceMock;
      groupService = groupServiceMock;

      router = routerMock;

      userServiceMock.getCurrentUserPermissions.and.returnValue(of(mockUserPermissionResponse));
      userServiceMock.getUser.and.returnValue(of(mockUserResponse));
      userServiceMock.getUserRoles.and.returnValue(of(mockRolesResponse));
      userServiceMock.addRolesToUser.and.returnValue(of(mockUserResponse));
      userServiceMock.removeRolesFromUser.and.returnValue(of(mockUserResponse));

      groupServiceMock.search.and.returnValue(of(mockGroupsResponse));
      groupServiceMock.getGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.getGroupRoles.and.returnValue(of(mockRolesResponse));
      groupServiceMock.getGroupUsers.and.returnValue(of(mockUsersResponse));
      groupServiceMock.addRolesToGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.removeRolesFromGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.addRolesToGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.addUsersToCustomGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.removeUserFromCustomGroup.and.returnValue(of(mockGroupResponse));
      groupServiceMock.getChildGroups.and.returnValue(of([]));
      groupServiceMock.addChildGroups.and.returnValue(of(mockGroupResponse));
      groupServiceMock.removeChildGroups.and.returnValue(of(mockGroupResponse));

      currentUserServiceMock.getPermissions.and.returnValue(of(mockCurrentUserPermissions));
    }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('search users', () => {
    it('returns groups on search', async(() => {
      // act
      component.searchTermSubject.next('bar');
      IdpSearchResultsSubject.next(mockExternalIdpSearchResult);

      // assert
      expect(searchService.search).toHaveBeenCalled();
      expect(component.principals.length).toBe(mockExternalIdpSearchResult.principals.length);
    }));

    it('adds a user and a group when none have returned', async(() => {
      // act
      component.searchTerm = 'asdf';
      IdpSearchResultsSubject.next({ resultCount: 0, principals: [] });

      // assert
      expect(component.principals.length).toBe(2);
      expect(component.principals[0].subjectId).toBe('asdf');
    }));
  });

  describe('associateUsersAndGroups', () => {
    it('returns groups on search', async(() => {
      // arrange
      component.principals = mockExternalIdpSearchResult.principals;
      component.principals[0].selected = true;

      // act
      component.associateUsersAndGroups();

      // assert
      expect(component.principals.length).toBe(mockExternalIdpSearchResult.principals.length - 1);
      expect(component.associatedUsers.length).toBe(1);
    }));
  });

  describe('unAssociateUsersAndGroups', () => {
    it('returns groups on search', async(() => {
      // arrange
      component.principals = mockExternalIdpSearchResult.principals;
      component.associatedUsers.push({
        subjectId: 'sub789',
        selected: true
      });

      // act
      component.unAssociateUsersAndGroups();

      // assert
      expect(component.principals.length).toBe(mockExternalIdpSearchResult.principals.length + 1);
    }));
  });

  describe('save', () => {
    it('returns descriptive error when groupname conflict', () => {
      // arrange
      const mockErrorResponse = {
          statusCode: 409,
          message: `Could not create group name "${mockGroupsResponse[0].groupName}". `
          + `A group with the same name exists as a Custom group or a Directory group.`
      };
      groupService.getChildGroups.and.returnValue(observableThrowError(mockErrorResponse));
      component.editMode = true;
      component.groupName = mockGroupsResponse[0].groupName;
      component.displayName = mockGroupsResponse[0].displayName;

      // act
      component.save();

      // assert
      expect(component.groupNameInvalid).toBe(true);
      expect(component.groupNameError).toBeTruthy();
      expect(component.groupNameError).toMatch(`Could not create group name "${mockGroupsResponse[0].groupName}". `
        + `A group with the same name exists as a Custom group or a Directory group.`);
    });

    it('returns error if associated group name is the same as existing custom group', () => {
      // arrange
      const mockErrorResponse = {
        statusCode: 409,
        message: `The associated user or group name should not be the same as an existing custom group: ${mockGroupsResponse[0].groupName}`
      };
      groupService.getChildGroups.and.returnValue(observableThrowError(mockErrorResponse));
      component.editMode = true;
      component.groupName = mockGroupsResponse[0].groupName;
      component.displayName = mockGroupsResponse[0].displayName;

      // act
      component.save();

      // assert
      expect(component.groupNameInvalid).toBe(true);
      expect(component.associatedNameError).toBeTruthy();
      expect(component.associatedNameError).toMatch(`The associated user or group name should not be the same as an existing custom group: `
        + `${mockGroupsResponse[0].groupName}`);
    });

    it('returns error if associated user name is the same as existing custom group', () => {
      // arrange
      const mockErrorResponse = {
        statusCode: 409,
        message: `The associated user or group name should not be the same as an existing custom group: `
          + `${mockUsersResponse[0].identityProviderUserPrincipalName}`
      };
      groupService.getChildGroups.and.returnValue(observableThrowError(mockErrorResponse));
      component.editMode = true;
      component.groupName = mockUsersResponse[0].name;
      component.displayName = mockUsersResponse[0].name;

      // act
      component.save();

      // assert
      expect(component.groupNameInvalid).toBe(true);
      expect(component.associatedNameError).toBeTruthy();
      expect(component.associatedNameError).toMatch(`The associated user or group name should not be the same as an existing custom group: `
        + `${mockUsersResponse[0].identityProviderUserPrincipalName}`);
    });

    it('dont reset the roles if group name invalid', () => {
      // Arrange
      component.groupNameSubject.next("test");
      // create a few roles for component.rolesForGrainAndSecurable
      component.rolesForGrainAndSecurable.map(r => r.selected = true)
      component.groupNameInvalid = true;
      component.groupNameSubject.next("test123456");

      // Act
      component.ngOnInit();

      // Assert
      expect(component.rolesForGrainAndSecurable[0].selected).toBe(true)
  });

  it('reset the roles if group name valid', () => {
    // Arrange
    component.groupNameSubject.next("test");
    // create a few roles for component.rolesForGrainAndSecurable
    component.rolesForGrainAndSecurable.map(r => r.selected = true)
    component.groupNameInvalid = false;
    component.groupNameSubject.next("test123456");

    // Act
    component.ngOnInit();

    // Assert
    expect(component.rolesForGrainAndSecurable[0].selected).toBe(false)
  });

  });

  describe('save button', () => {
    it('is enabled for an admin user', () => {
      // assert
      const saveButton: DebugElement = fixture.debugElement.query(By.css('#saveButton'));
      expect(saveButton.nativeElement.disabled).toBe(true);
    });
  });

  describe('getGroupNameToDisplay', () => {

    it('should return the display name if exist', () => {
      // Arrange
      const displayName = 'displayName';
      const group = {
        displayName: displayName,
        groupName: 'groupName',
        id: '1',
        roles: null,
        users: null,
        groupSource: '',
        description: '',
        children: null,
        parents: null
      };
      // Act
      const result = component.getGroupNameToDisplay(group);

      // Assert
      expect(result).toBe(displayName);
    });

    it('should return the group name if exist and display name is null', () => {
      // Arrange
      const groupName = 'groupName';
      const group = {
        displayName: null,
        groupName: groupName,
        id: '1',
        roles: null,
        users: null,
        groupSource: '',
        description: '',
        children: null,
        parents: null
      };
      // Act
      const result = component.getGroupNameToDisplay(group);

      // Assert
      expect(result).toBe(groupName);
    });

    it('should return the group name if exist and display name is undefined', () => {
      // Arrange
      const groupName = 'groupName';
      const group = {
        displayName: undefined,
        groupName: groupName,
        id: '1',
        roles: null,
        users: null,
        groupSource: '',
        description: '',
        children: null,
        parents: null
      };
      // Act
      const result = component.getGroupNameToDisplay(group);

      // Assert
      expect(result).toBe(groupName);
    });

    it('should return the group name if exist and display name is empty string', () => {
      // Arrange
      const groupName = '';
      const group = {
        displayName: undefined,
        groupName: groupName,
        id: '1',
        roles: null,
        users: null,
        groupSource: '',
        description: '',
        children: null,
        parents: null
      };
      // Act
      const result = component.getGroupNameToDisplay(group);

      // Assert
      expect(result).toBe(groupName);
    });
  });

  describe('getAdGroupNameToDisplay', () => {
    it('should append tenant alias if not null', () => {
      const groupName = 'group-name';
      const alias = 'alias-name';
      const group = {
        groupName: groupName,
        tenantAlias: alias,
        groupSource: ''
      };

      // act
      const result = component.getAdGroupNameToDisplay(group);

      // assert
      expect(result).toBe(groupName + '@' + alias);
    });

    it('should not append tenant alias if not null', () => {
      const groupName = 'group-name';
      const group = {
        groupName: groupName,
        groupSource: ''
      };

      // act
      const result = component.getAdGroupNameToDisplay(group);

      // assert
      expect(result).toBe(groupName);
    });
  });

  describe('syncErrors', () => {
    it('should show sync alert when sync user error occurs', () => {
      const mockErrorResponse = {
        statusCode: 400,
        message: 'sync error'
      };

      edwAdminService.syncUsersWithEdwAdmin.and.returnValue(observableThrowError(mockErrorResponse));
      edwAdminService.syncGroupWithEdwAdmin.and.returnValue(of({statusCode: 200}));

      spyOn(router, 'navigate').and.callFake((route: string) => {
        expect(alertService.showSyncWarning).toHaveBeenCalledTimes(1);
        expect(alertService.showError).toHaveBeenCalledTimes(0);
        return Promise.resolve(true);
      });

      const principal: IFabricPrincipal = {
        subjectId: 'HQCATALYST\\first.last',
        principalType: 'user',
        selected: true
      };

      component.editMode = true;
      component.groupName = mockGroupsResponse[0].groupName;
      component.displayName = mockGroupsResponse[0].displayName;
      component.principals = [principal];
      component.associateUsersAndGroups();
      component.save();
    });

    it('should show sync alert when sync group error occurs', () => {
      const mockErrorResponse = {
        statusCode: 400,
        message: 'sync error'
      };

      edwAdminService.syncUsersWithEdwAdmin.and.returnValue(of({statusCode: 200}));
      edwAdminService.syncGroupWithEdwAdmin.and.returnValue(observableThrowError(mockErrorResponse));

      spyOn(router, 'navigate').and.callFake((route: string) => {
        expect(alertService.showSyncWarning).toHaveBeenCalledTimes(1);
        expect(alertService.showError).toHaveBeenCalledTimes(0);
        return Promise.resolve(true);
      });

      const principal: IFabricPrincipal = {
        subjectId: 'HQCATALYST\\first.last',
        principalType: 'user',
        selected: true
      };

      component.editMode = true;
      component.groupName = mockGroupsResponse[0].groupName;
      component.displayName = mockGroupsResponse[0].displayName;
      component.principals = [principal];
      component.associateUsersAndGroups();
      component.save();
    });
  });
});
