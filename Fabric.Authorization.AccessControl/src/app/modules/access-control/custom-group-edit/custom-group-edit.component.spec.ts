import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { CustomGroupEditComponent } from './custom-group-edit.component';
import { ServicesMockModule } from '../services.mock.module';
import { FormsModule } from '@angular/forms';
import { FabricAuthGroupService, FabricAuthRoleService } from '../../../services';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { FabricAuthGroupServiceMock, mockUsersResponse } from '../../../services/fabric-auth-group.service.mock';
import { Observable } from 'rxjs/Observable';
import { mockRolesResponse } from '../../../services/fabric-auth-user.service.mock';

describe('CustomGroupEditComponent', () => {
  let component: CustomGroupEditComponent;
  let fixture: ComponentFixture<CustomGroupEditComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupEditComponent],
        imports: [FormsModule, ServicesMockModule]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthGroupService, FabricAuthRoleService], (groupService: FabricAuthGroupServiceMock,
      roleService: FabricAuthRoleServiceMock) => {
    groupService.getGroupUsers.and.returnValue(Observable.of(mockUsersResponse));
    groupService.getGroupRoles.and.returnValue(Observable.of(mockRolesResponse));
    roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRolesResponse));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
