import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { CustomGroupComponent } from './custom-group.component';
import { ServicesMockModule } from '../services.mock.module';
import { FormsModule } from '@angular/forms';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { FabricAuthGroupServiceMock, mockUsersResponse } from '../../../services/fabric-auth-group.service.mock';
import { Observable } from 'rxjs/Observable';
import { mockRolesResponse } from '../../../services/fabric-auth-user.service.mock';
import { ButtonModule, IconModule, PopoverModule, InputModule, LabelModule, CheckboxModule } from '@healthcatalyst/cashmere';
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';

describe('CustomGroupComponent', () => {
  let component: CustomGroupComponent;
  let fixture: ComponentFixture<CustomGroupComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupComponent],
        imports: [FormsModule,
          ServicesMockModule,
          ButtonModule,
          IconModule,
          PopoverModule,
          InputModule,
          LabelModule,
          CheckboxModule]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthGroupService, FabricAuthRoleService, FabricExternalIdpSearchService],
    (groupService: FabricAuthGroupServiceMock, roleService: FabricAuthRoleServiceMock, search: FabricExternalIdpSearchServiceMock) => {
      groupService.getGroupUsers.and.returnValue(Observable.of(mockUsersResponse));
      groupService.getGroupRoles.and.returnValue(Observable.of(mockRolesResponse));
      roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRolesResponse));
      search.search.and.returnValue(Observable.of(mockExternalIdpSearchResult));
    }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
