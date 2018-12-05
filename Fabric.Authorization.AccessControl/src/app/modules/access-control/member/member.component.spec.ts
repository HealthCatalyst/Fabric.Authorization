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
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';
import { ServicesMockModule } from '../services.mock.module';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { CurrentUserServiceMock, mockCurrentUserPermissions } from '../../../services/current-user.service.mock';
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

describe('MemberAddComponent', () => {
  let component: MemberComponent;
  let fixture: ComponentFixture<MemberComponent>;
  let edwAdminService: FabricAuthEdwadminServiceMock;
  let alertService: AlertServiceMock;
  let userService: FabricAuthUserServiceMock;
  let groupService: FabricAuthGroupServiceMock;

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
        currentUserServiceMock.getPermissions.and.returnValue(of(mockCurrentUserPermissions));
        roleService.getRolesBySecurableItemAndGrain.and.returnValue(of(mockRolesResponse));

        edwAdminService = edwAdminServiceMock;
        alertService = alertServiceMock;
        userService = userServiceMock;
        groupService = groupServiceMock;
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

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
    component.saveUser(principal.subjectId, mockRolesResponse).subscribe(() => {
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
