import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';

import { CustomGroupComponent } from './custom-group.component';
import { ServicesMockModule } from '../services.mock.module';
import { FormsModule } from '@angular/forms';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthRoleServiceMock, mockRoles } from '../../../services/fabric-auth-role.service.mock';
import { FabricAuthGroupServiceMock, mockUsersResponse, mockGroupsResponse } from '../../../services/fabric-auth-group.service.mock';
import { Observable } from 'rxjs/Observable';
import { mockRolesResponse } from '../../../services/fabric-auth-user.service.mock';
import { ButtonModule, IconModule, PopoverModule, InputModule, LabelModule, CheckboxModule } from '@healthcatalyst/cashmere';
import { FabricExternalIdpSearchServiceMock, mockExternalIdpSearchResult } from '../../../services/fabric-external-idp-search.service.mock';
import { IdPSearchResult } from '../../../models/idpSearchResult.model';
import { Subject } from 'rxjs/Subject';

describe('CustomGroupComponent', () => {
  let component: CustomGroupComponent;
  let fixture: ComponentFixture<CustomGroupComponent>;
  let IdpSearchResultsSubject: Subject<IdPSearchResult>;
  let searchService: FabricExternalIdpSearchServiceMock;

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
      groupService.search.and.returnValue(Observable.of(mockGroupsResponse));

      roleService.getRolesBySecurableItemAndGrain.and.returnValue(Observable.of(mockRolesResponse));
      searchService = search;

      IdpSearchResultsSubject = new Subject<IdPSearchResult>();
        searchService.search.and.callFake((searchText: Observable<string>, type: string) => {
            return IdpSearchResultsSubject;
        });
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

    it('adds a user when none have returned', async(() => {
        // act
        component.searchTerm = 'asdf';
        IdpSearchResultsSubject.next({resultCount: 0, principals: []});

        // assert
        expect(component.principals.length).toBe(1);
        expect(component.principals[0].subjectId).toBe('asdf');
    }));
  });

  describe('associateUsers', () => {
    it('returns groups on search', async(() => {
        // arrange
        component.principals = mockExternalIdpSearchResult.principals;
        component.principals[0].selected = true;

        // act
        component.associateUsers();

        // assert
        expect(component.principals.length).toBe(mockExternalIdpSearchResult.principals.length - 1);
        expect(component.associatedPrincipals.length).toBe(1);
    }));
  });

  describe('unAssociateUsers', () => {
    it('returns groups on search', async(() => {
        // arrange
        component.principals = mockExternalIdpSearchResult.principals;
        component.associatedPrincipals.push({
            subjectId: 'sub789',
            selected: true
        });

        // act
        component.unAssociateUsers();

        // assert
        expect(component.principals.length).toBe(mockExternalIdpSearchResult.principals.length + 1);
    }));
  });
});
