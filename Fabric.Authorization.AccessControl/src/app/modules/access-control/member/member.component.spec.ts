import { Observable, of } from 'rxjs';
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
import { FabricAuthUserServiceMock, mockUserPermissionResponse } from '../../../services/fabric-auth-user.service.mock';

describe('MemberAddComponent', () => {
  let component: MemberComponent;
  let fixture: ComponentFixture<MemberComponent>;

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
    CurrentUserService],
      (roleService: FabricAuthRoleServiceMock,
      idpSearch: FabricExternalIdpSearchServiceMock,
      userService: FabricAuthUserServiceMock,
      currentUserServiceMock: CurrentUserServiceMock) => {
        roleService.getRolesBySecurableItemAndGrain.and.returnValue(of(mockRoles));
        idpSearch.search.and.returnValue(of(mockExternalIdpSearchResult));
        userService.getCurrentUserPermissions.and.returnValue(of(mockUserPermissionResponse));
        currentUserServiceMock.getPermissions.and.returnValue(of(mockCurrentUserPermissions));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
