import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { MemberEditComponent } from './member-edit.component';
import { ServicesMockModule } from '../member-add/services.mock.module';
import { FabricAuthGroupService, FabricAuthRoleService } from '../../../services';
import { FabricAuthGroupServiceMock } from '../../../services/fabric-auth-group.service.mock';
import { Observable } from 'rxjs/Observable';
import { mockRolesResponse } from '../../../services/fabric-auth-user.service.mock';
import { FabricAuthRoleServiceMock } from '../../../services/fabric-auth-role.service.mock';

describe('MemberEditComponent', () => {
  let component: MemberEditComponent;
  let fixture: ComponentFixture<MemberEditComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [MemberEditComponent],
        imports: [ServicesMockModule]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthGroupService, FabricAuthRoleService], (groupService: FabricAuthGroupServiceMock,
      roleService: FabricAuthRoleServiceMock) => {
    groupService.getGroupRoles.and.returnValue(Observable.of(mockRolesResponse));
    roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRolesResponse));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MemberEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
