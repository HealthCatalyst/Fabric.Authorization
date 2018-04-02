import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { CustomGroupAddComponent } from './custom-group-add.component';
import { FormsModule } from '@angular/forms';
import { FabricExternalIdpSearchService, AccessControlConfigService, FabricAuthRoleService } from '../../../services';
import { HttpClient, HttpHandler } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { ServicesMockModule } from '../services.mock.module';

describe('CustomGroupAddComponent', () => {
  let component: CustomGroupAddComponent;
  let fixture: ComponentFixture<CustomGroupAddComponent>;

  beforeEach(
    async(() => {
      TestBed.configureTestingModule({
        declarations: [CustomGroupAddComponent],
        imports: [ServicesMockModule, FormsModule],
        providers: [FabricExternalIdpSearchService, HttpClient, HttpHandler, AccessControlConfigService]
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthRoleService], (roleService: FabricAuthRoleServiceMock) => {
    roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRoles));
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomGroupAddComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
