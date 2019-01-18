import { of, throwError } from 'rxjs';
import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { ToastrModule } from 'ngx-toastr';

import { MemberComponent } from './member.component';
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule,
  ProgressIndicatorsModule
} from '@healthcatalyst/cashmere';
import { FormsModule } from '@angular/forms';
import {
    FabricExternalIdpSearchServiceMock,
    mockExternalIdpSearchResult,
    mockExternalIdpGroupSearchResult
  } from '../../../services/fabric-external-idp-search.service.mock';
import { ServicesMockModule } from '../services.mock.module';
import { FabricAuthRoleServiceMock } from '../../../services/fabric-auth-role.service.mock';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { CurrentUserServiceMock, mockCurrentUserPermissions, mockAdminUserPermissions } from '../../../services/current-user.service.mock';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { CurrentUserService } from '../../../services/current-user.service';
import { FabricAuthUserServiceMock, mockUserPermissionResponse, mockUserResponse } from '../../../services/fabric-auth-user.service.mock';
import { FabricAuthEdwAdminService } from '../../../services/fabric-auth-edwadmin.service';
import { FabricAuthEdwadminServiceMock } from '../../../services/fabric-auth-edwadmin.service.mock';
import { AlertServiceMock } from '../../../services/global/alert.service.mock';
import { AlertService } from '../../../services/global/alert.service';
import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { mockRolesResponse, FabricAuthGroupServiceMock, mockGroupResponse } from '../../../services/fabric-auth-group.service.mock';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

describe('MemberComponent', () => {
  let component: MemberComponent;
  let fixture: ComponentFixture<MemberComponent>;
  let edwAdminService: FabricAuthEdwadminServiceMock;
  let alertService: AlertServiceMock;
  let userService: FabricAuthUserServiceMock;
  let groupService: FabricAuthGroupServiceMock;
  let idpSearchService: FabricExternalIdpSearchServiceMock;
  let currentUserService: CurrentUserServiceMock;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [MemberComponent],
        imports: [ServicesMockModule,
          FormsModule,
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
              provide: ActivatedRoute, useValue: {
                snapshot: {
                  paramMap: convertToParamMap({
                    grain: 'dos',
                    securableItem: 'datamarts'
                  })
                },
                queryParams: of({})
              }
            }
          ]
      }).compileComponents();
    })
  );

  beforeEach(inject([
    FabricAuthRoleService,
    FabricExternalIdpSearchService,
    FabricAuthUserService,
    CurrentUserService,
    FabricAuthEdwAdminService,
    FabricAuthGroupService,
    AlertService],
      (roleService: FabricAuthRoleServiceMock,
      idpSearch: FabricExternalIdpSearchServiceMock,
      userServiceMock: FabricAuthUserServiceMock,
      currentUserServiceMock: CurrentUserServiceMock,
      edwAdminServiceMock: FabricAuthEdwadminServiceMock,
      groupServiceMock: FabricAuthGroupServiceMock,
      alertServiceMock: AlertServiceMock) => {
        idpSearch.search.and.returnValue(of(mockExternalIdpSearchResult));
        userServiceMock.getCurrentUserPermissions.and.returnValue(of(mockUserPermissionResponse));
        currentUserServiceMock.getPermissions.and.returnValue(of(mockAdminUserPermissions));
        roleService.getRolesBySecurableItemAndGrain.and.returnValue(of(mockRolesResponse));

        edwAdminService = edwAdminServiceMock;
        alertService = alertServiceMock;
        userService = userServiceMock;
        currentUserService = currentUserServiceMock;
        groupService = groupServiceMock;
        idpSearchService = idpSearch;
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('sync errors', () => {
    it('should show sync alert when sync user error occurs', () => {
      const mockErrorResponse = {
        statusCode: 400,
        message: 'sync error'
      };

      edwAdminService.syncUsersWithEdwAdmin.and.returnValue(throwError(mockErrorResponse));
      userService.getUser.and.returnValue(of(mockUserResponse));
      userService.getUserRoles.and.returnValue(of(mockRolesResponse));
      userService.addRolesToUser.and.returnValue(of(mockUserResponse));
      userService.removeRolesFromUser.and.returnValue(of(mockUserResponse));

      const principal: IFabricPrincipal = {
        subjectId: 'HQCATALYST\\first.last',
        principalType: 'user'
      };

      component.selectPrincipal(principal);
      component.saveUser(principal, mockRolesResponse).subscribe(() => {
        expect(alertService.showSyncWarning).toHaveBeenCalledTimes(1);
        expect(alertService.showError).toHaveBeenCalledTimes(0);
      });
    });

    it('should show sync alert when sync group error occurs', () => {
      const mockErrorResponse = {
        statusCode: 400,
        message: 'sync error'
      };

      edwAdminService.syncGroupWithEdwAdmin.and.returnValue(throwError(mockErrorResponse));
      groupService.getGroup.and.returnValue(of(mockGroupResponse));
      groupService.getGroupRoles.and.returnValue(of(mockRolesResponse));
      groupService.addRolesToGroup.and.returnValue(of(mockGroupResponse));
      groupService.removeRolesFromGroup.and.returnValue(of(mockGroupResponse));

      const principal: IFabricPrincipal = {
        subjectId: 'HQCATALYST\\product.development',
        principalType: 'group'
      };

      component.selectPrincipal(principal);
      component.saveGroup(principal, mockRolesResponse).subscribe(() => {
        expect(alertService.showSyncWarning).toHaveBeenCalledTimes(1);
        expect(alertService.showError).toHaveBeenCalledTimes(0);
      });
    });
  });

  describe('member selection', () => {
    describe('users', () => {
      it('associate a user when selected', () => {
        // arrange
        userService.getUserRoles.and.returnValue(of(mockRolesResponse));
        fixture.detectChanges();
        const idpssPrincipals = fixture.debugElement.queryAll(By.css('li'));

        // act - select first user
        expect(idpssPrincipals.length).toEqual(mockExternalIdpSearchResult.principals.length);
        const checkBox = idpssPrincipals[0].query(By.css('hc-checkbox'));
        checkBox.triggerEventHandler('click', {});

        // assert
        const expected = mockExternalIdpSearchResult.principals[0].identityProviderUserPrincipalName;
        expect(component.selectedPrincipal.identityProviderUserPrincipalName).toEqual(expected);
      });
    });

    describe('ad groups', () => {
      it('should associate a group when selected', () => {
        // arrange
        idpSearchService.search.and.returnValue(of(mockExternalIdpGroupSearchResult));
        groupService.getGroupRoles.and.returnValue(of(mockRolesResponse));

        // recreate component with group results search
        fixture = TestBed.createComponent(MemberComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

        const idpssPrincipals = fixture.debugElement.queryAll(By.css('li'));

        // act - select first group
        expect(idpssPrincipals.length).toEqual(mockExternalIdpGroupSearchResult.principals.length);
        const checkBox = idpssPrincipals[0].query(By.css('hc-checkbox'));
        checkBox.triggerEventHandler('click', {});
        fixture.detectChanges();

        // assert
        const expected = mockExternalIdpGroupSearchResult.principals[0].subjectId;
        expect(component.selectedPrincipal.subjectId).toEqual(expected);
      });
    });

    describe('save button', () => {

      it('should not be disabled', () => {
        // assert
        expect(component.disabledSaveReason).toEqual('');
      });

      it('should display necessary permissions', () => {
        // arrange/act - create component with no permissions on current user
        currentUserService.getPermissions.and.returnValue(of(mockCurrentUserPermissions));
        fixture = TestBed.createComponent(MemberComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

        // assert
        expect(component.disabledSaveReason).toEqual('You are missing the following required permissions to edit: '
          + 'dos/datamarts.manageauthorization');
      });
    });
  });
});
