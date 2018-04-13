import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { MemberComponent } from './member.component';
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule
} from '@healthcatalyst/cashmere';
import { FormsModule } from '@angular/forms';
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';
import { ServicesMockModule } from '../services.mock.module';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { Observable } from 'rxjs/Observable';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';

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
          CheckboxModule],
      }).compileComponents();
    })
  );

  beforeEach(inject([FabricAuthRoleService, FabricExternalIdpSearchService], (roleService: FabricAuthRoleServiceMock,
    idpSearch: FabricExternalIdpSearchServiceMock) => {
    roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRoles));
    idpSearch.search.and.returnValue(Observable.of(mockExternalIdpSearchResult));
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
