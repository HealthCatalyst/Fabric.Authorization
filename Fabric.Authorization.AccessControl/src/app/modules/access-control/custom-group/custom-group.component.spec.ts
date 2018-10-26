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
import { FabricAuthGroupServiceMock, mockUsersResponse, mockGroupsResponse } from '../../../services/fabric-auth-group.service.mock';
import { mockRolesResponse, FabricAuthUserServiceMock, mockUserPermissionResponse } from '../../../services/fabric-auth-user.service.mock';
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule,
  ProgressIndicatorsModule
} from '@healthcatalyst/cashmere';
import { InputDirective } from '../input.directive'
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';
import { IdPSearchResult } from '../../../models/idpSearchResult.model';
import { CurrentUserServiceMock, mockCurrentUserPermissions } from '../../../services/current-user.service.mock';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { CurrentUserService } from '../../../services/current-user.service';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';

describe('CustomGroupComponent', () => {
  let component: CustomGroupComponent;
  let fixture: ComponentFixture<CustomGroupComponent>;
  let IdpSearchResultsSubject: Subject<IdPSearchResult>;
  let searchService: FabricExternalIdpSearchServiceMock;
  let groupService: FabricAuthGroupServiceMock;

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
          ToastrModule.forRoot()]
      }).compileComponents();
    })
  );

  beforeEach(inject([
    FabricAuthGroupService,
    FabricAuthRoleService,
    FabricExternalIdpSearchService,
    FabricAuthUserService,
    CurrentUserService],
    (group: FabricAuthGroupServiceMock,
      roleService: FabricAuthRoleServiceMock,
      search: FabricExternalIdpSearchServiceMock,
      userService: FabricAuthUserServiceMock,
      currentUserServiceMock: CurrentUserServiceMock) => {
      group.getGroupUsers.and.returnValue(of(mockUsersResponse));
      group.getGroupRoles.and.returnValue(of(mockRolesResponse));
      group.search.and.returnValue(of(mockGroupsResponse));
      groupService = group;

      roleService.getRolesBySecurableItemAndGrain.and.returnValue(of(mockRolesResponse));
      searchService = search;

      IdpSearchResultsSubject = new Subject<IdPSearchResult>();
      searchService.search.and.callFake((searchText: Observable<string>, type: string) => {
        return IdpSearchResultsSubject;
      });

      userService.getCurrentUserPermissions.and.returnValue(of(mockUserPermissionResponse));
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

  describe('associateUsers', () => {
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

  describe('unAssociateUsers', () => {
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
          statusCode: 409
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
      expect(component.groupNameError).toMatch(`Could not create group named "${mockGroupsResponse[0].groupName}"\.*`);
    });
  });

  describe('save button', () => {
    it('is enabled for an admin user', () => {
      // assert
      let saveButton: DebugElement = fixture.debugElement.query(By.css('#saveButton'));
      expect(saveButton.nativeElement.disabled).toBe(true);
    })
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
});
