import { HttpClient, HttpErrorResponse, HttpHeaders, HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Rx';

import { FabricAuthUserService, AccessControlConfigService } from '../services';
import { Group, User, Role } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';

fdescribe('FabricAuthUserService', () => {

  const idP = 'ad';
  const subjectId = 'sub123';

  const mockUserGroupsResponse = [
    'Group 1',
    'Group 2'
  ];

  const mockUserRolesResponse = [
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
  ];

  const mockUserResponse = {
    id: idP,
    name: 'First Last',
    identityProvider: idP,
    subjectId: subjectId,
    groups: mockUserGroupsResponse,
    roles: mockUserRolesResponse
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [
        FabricAuthUserService,         
        { provide: HTTP_INTERCEPTORS, useClass: FabricHttpErrorHandlerInterceptorService, multi: true },
        AccessControlConfigService ]
    });
  });

  it('getUserGroups should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.getUserGroups(idP, subjectId).subscribe(returnedGroups => {
          assertMockUserGroupsResponse(returnedGroups);
        });

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/groups`));
        expect(req.request.method).toBe("GET");      
        req.flush(mockUserGroupsResponse, {status: 200, statusText: 'OK'});        
        httpTestingController.verify();
      })
    )
  );

  it('getUserGroups error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.getUserGroups(idP, subjectId).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('User not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/groups`));
        expect(req.request.method).toBe("GET");
        req.flush(null, {status: 404, statusText: 'User not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('getUserRoles should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.getUserRoles(idP, subjectId).subscribe(returnedRoles => {
          assertMockUserRolesResponse(returnedRoles);
        });

        const req = httpTestingController.expectOne(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`);
        expect(req.request.method).toBe("GET");      
        req.flush(mockUserRolesResponse, {status: 200, statusText: 'OK'});        
        httpTestingController.verify();
      })
    )
  );

  it('getUserRoles error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.getUserRoles(idP, subjectId).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('User not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`));
        expect(req.request.method).toBe("GET");
        req.flush(null, {status: 404, statusText: 'User not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('addRolesToUser should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.addRolesToUser(idP, subjectId, null).subscribe(returnedUser => {
          assertMockUserResponse(returnedUser);
        });

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`));
        expect(req.request.method).toBe("POST");      
        req.flush(mockUserResponse, {status: 201, statusText: 'Created'});
        httpTestingController.verify();
      })
    )
  );

  it('addRolesToUser error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.addRolesToUser(idP, subjectId, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('User not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`));
        expect(req.request.method).toBe("POST");
        req.flush(null, {status: 404, statusText: 'User not found'});        
        httpTestingController.verify();
      })
    )
  );

  it('removeRolesFromUser should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.removeRolesFromUser(idP, subjectId, null).subscribe(returnedUser => {
          assertMockUserResponse(returnedUser);
        });

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`));
        expect(req.request.method).toBe("DELETE");      
        req.flush(mockUserResponse, {status: 204, statusText: 'No Content'});
        httpTestingController.verify();
      })
    )
  );

  it('removeRolesFromUser error should be caught',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthUserService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthUserService) => {

        service.removeRolesFromUser(idP, subjectId, null).catch(error => {
          expect(Observable.of(error)).toBeTruthy();
          expect(error.statusCode).toBe(404);
          expect(error.message).toBe('User not found');
          return Observable.of(error);
        })
        .subscribe();

        const req = httpTestingController.expectOne(encodeURI(`${FabricAuthUserService.baseUserApiUrl}/${idP}/${subjectId}/roles`));
        expect(req.request.method).toBe("DELETE");
        req.flush(null, {status: 404, statusText: 'User not found'});        
        httpTestingController.verify();
      })
    )
  );

  function assertMockUserResponse(returnedUser: User) {
    expect(returnedUser.id).toBe(idP);
    expect(returnedUser.name).toBe('First Last');
    expect(returnedUser.subjectId).toBe(subjectId);
    assertMockUserGroupsResponse(returnedUser.groups);
    assertMockUserRolesResponse(returnedUser.roles);
  }

  function assertMockUserGroupsResponse(returnedGroups: string[]) {
    expect(returnedGroups).toBeDefined();
    expect(returnedGroups[0]).toBe('Group 1');
    expect(returnedGroups[1]).toBe('Group 2');
  }

  function assertMockUserRolesResponse(returnedRoles: Role[]) {
    expect(returnedRoles).toBeDefined();
    expect(returnedRoles.length).toBe(2);

    let adminRole = returnedRoles[0];
    expect(adminRole.name).toBe('admin');
    expect(adminRole.grain).toBe('dos');
    expect(adminRole.securableItem).toBe('datamart');
    expect(adminRole.parentRole).toBe('admin_parent');

    let superUserRole = returnedRoles[1];
    expect(superUserRole.name).toBe('superuser');
    expect(superUserRole.grain).toBe('dos');
    expect(superUserRole.securableItem).toBe('datamart');
    expect(superUserRole.childRoles).toBeDefined();
    expect(superUserRole.childRoles.length).toBe(2);
    expect(superUserRole.childRoles[0]).toBe('dos_child1');
    expect(superUserRole.childRoles[1]).toBe('dos_child2');
  }
});
