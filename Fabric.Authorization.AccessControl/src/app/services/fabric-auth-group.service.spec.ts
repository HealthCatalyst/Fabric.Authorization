import { HttpClient, HttpErrorResponse, HttpHeaders, HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Rx';

import { FabricAuthGroupService, AccessControlConfigService } from '../services';
import { Group, User, Role } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';

fdescribe('FabricAuthGroupService', () => {

  const mockGroupUsersResponse = {
    groupName: 'Group 1',
    groupSource: 'Custom',
    users: [
      {
        name: 'First Last',
        subjectId: 'Sub123',
        identityProvider: 'Windows'
      }
    ]
  };

  const mockGroupRolesResponse = {
    groupName: 'Dos Admin Group',
    groupSource: 'Custom',
    roles: [
      {
        name: 'admin',
        grain: 'dos',
        securableItem: 'datamart',
        parentRole: 'admin_parent'
      },
      {
        name: 'superuser',
        grain: 'dos',
        securableItem: 'datamart',
        childRoles: [
          'dos_child1',
          'dos_child2'
        ]
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [
        FabricAuthGroupService,         
        { provide: HTTP_INTERCEPTORS, useClass: FabricHttpErrorHandlerInterceptorService, multi: true },
        AccessControlConfigService ]
    });
  });

  it('should be created', inject([FabricAuthGroupService], (service: FabricAuthGroupService) => {
    expect(service).toBeTruthy();
  }));

  it('getGroupUsers should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.getGroupUsers(mockGroupRolesResponse.groupName).subscribe(returnedGroup => {
          assertMockGroupUsersResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/users`);
        expect(req.request.method).toBe("GET");      
        req.flush(mockGroupUsersResponse, {status: 200, statusText: 'OK'});        
        httpTestingController.verify();
      })
    )
  );

  it('getGroupUsers error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.getGroupUsers(mockGroupUsersResponse.groupName).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUsersResponse.groupName}/users`);
        expect(req.request.method).toBe("GET");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('addUserToCustomGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.addUserToCustomGroup(mockGroupUsersResponse.groupName, null).subscribe(returnedGroup => {
          assertMockGroupUsersResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUsersResponse.groupName}/users`);
        expect(req.request.method).toBe("POST");
        req.flush(mockGroupUsersResponse, {status: 201, statusText: 'Created'});        
        httpTestingController.verify();
      })
    )
  );

  it('addUserToCustomGroup error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.addUserToCustomGroup(mockGroupUsersResponse.groupName, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUsersResponse.groupName}/users`);
        expect(req.request.method).toBe("POST");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('removeUserFromCustomGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.removeUserFromCustomGroup(mockGroupUsersResponse.groupName, null).subscribe(returnedGroup => {
          assertMockGroupUsersResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupUsersResponse.groupName}/users`);
        expect(req.request.method).toBe("DELETE");  
        req.flush(mockGroupUsersResponse, {status: 204, statusText: 'No Content'});        
        httpTestingController.verify();
      })
    )
  );

  it('removeUserFromCustomGroup error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.removeUserFromCustomGroup(mockGroupRolesResponse.groupName, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/users`);
        expect(req.request.method).toBe("DELETE");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('getGroupRoles should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.getGroupRoles(mockGroupRolesResponse.groupName).subscribe(returnedGroup => {
          assertMockGroupRolesResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("GET");      
        req.flush(mockGroupRolesResponse, {status: 200, statusText: 'OK'});        
        httpTestingController.verify();
      })
    )
  );

  it('getGroupRoles error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.getGroupRoles(mockGroupRolesResponse.groupName, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("GET");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('addRoleToGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {
        
        service.addRoleToGroup(mockGroupRolesResponse.groupName, null).subscribe(returnedGroup => {
          assertMockGroupRolesResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("POST");      
        req.flush(mockGroupRolesResponse, {status: 201, statusText: 'Created'});
        httpTestingController.verify();
      })
    )
  );

  it('addRoleToGroup error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.addRoleToGroup(mockGroupRolesResponse.groupName, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("POST");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('removeRoleFromGroup should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.removeRoleFromGroup(mockGroupRolesResponse.groupName, null).subscribe(returnedGroup => {
          assertMockGroupRolesResponse(returnedGroup);
        });

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("DELETE");      
        req.flush(mockGroupRolesResponse, {status: 204, statusText: 'No Content'});        
        httpTestingController.verify();
      })
    )
  );

  it('removeRoleFromGroup error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthGroupService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthGroupService) => {

        service.removeRoleFromGroup(mockGroupRolesResponse.groupName, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('Group not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(`${FabricAuthGroupService.baseGroupApiUrl}/${mockGroupRolesResponse.groupName}/roles`);
        expect(req.request.method).toBe("DELETE");
        req.flush(null, {status: 404, statusText: 'Group not found'});        
        httpTestingController.verify();
      })
    )
  );

  function assertMockGroupRolesResponse(returnedGroup: Group) {
    expect(returnedGroup).toBeDefined();
    expect(returnedGroup.groupName).toBe('Dos Admin Group');
    expect(returnedGroup.groupSource).toBe('Custom');
    expect(returnedGroup.roles).toBeDefined();
    expect(returnedGroup.roles.length).toBe(2);

    let adminRole = returnedGroup.roles[0];
    expect(adminRole.name).toBe('admin');
    expect(adminRole.grain).toBe('dos');
    expect(adminRole.securableItem).toBe('datamart');
    expect(adminRole.parentRole).toBe('admin_parent');

    let superUserRole = returnedGroup.roles[1];
    expect(superUserRole.name).toBe('superuser');
    expect(superUserRole.grain).toBe('dos');
    expect(superUserRole.securableItem).toBe('datamart');
    expect(superUserRole.childRoles).toBeDefined();
    expect(superUserRole.childRoles.length).toBe(2);
    expect(superUserRole.childRoles[0]).toBe('dos_child1');
    expect(superUserRole.childRoles[1]).toBe('dos_child2');
  }

  function assertMockGroupUsersResponse(returnedGroup: Group) {
    expect(returnedGroup.groupName).toBe(mockGroupUsersResponse.groupName);
    expect(returnedGroup.groupSource).toBe(mockGroupUsersResponse.groupSource);
    expect(returnedGroup.users).toBeDefined();
    expect(returnedGroup.users.length).toEqual(1);
    let returnedUser = returnedGroup.users[0];
    let originalUser = mockGroupUsersResponse.users[0];
    expect(returnedUser.subjectId).toEqual(originalUser.subjectId);
    expect(returnedUser.identityProvider).toEqual(originalUser.identityProvider);
    expect(returnedUser.name).toEqual(originalUser.name);
  }
});
