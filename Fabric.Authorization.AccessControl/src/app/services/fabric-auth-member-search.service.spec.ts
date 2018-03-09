import { HttpClient, HttpErrorResponse, HttpHeaders, HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Rx';

import { FabricAuthMemberSearchService, AccessControlConfigService } from '../services';
import { Group, User, Role, AuthMemberSearchRequest } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';

fdescribe('FabricAuthMemberSearchService', () => {

  const mockAuthSearchResult = [
    {
      subjectId: 'sub123',
      identityProvider: 'AD',
      firstName: 'First',
      lastName: 'Last',
      roles: [
        'admin',
        'superuser'
      ],
      entityType: "user"
    },
    {
      groupName: 'Group 1',
      roles: [
        'viewer'
      ],
      entityType: 'group'
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [
        FabricAuthMemberSearchService,         
        { provide: HTTP_INTERCEPTORS, useClass: FabricHttpErrorHandlerInterceptorService, multi: true },
        AccessControlConfigService ]
    });
  });

  it('should be created', inject([FabricAuthMemberSearchService], (service: FabricAuthMemberSearchService) => {
    expect(service).toBeTruthy();
  }));

  it('searchMembers should deserialize all properties',
    async(  
      inject([HttpClient, HttpTestingController, FabricAuthMemberSearchService], (
        httpClient: HttpClient,
        httpTestingController: HttpTestingController,
        service: FabricAuthMemberSearchService) => {

        let authSearchRequest = new AuthMemberSearchRequest();
        authSearchRequest.clientId = 'atlas';
        //authSearchRequest.pageNumber = 1;

        service.searchMembers(authSearchRequest).subscribe(searchResults => {
          expect(searchResults).toBeDefined();
          expect(searchResults.length).toBe(2);

          let result1 = searchResults[0];
          expect(result1.subjectId).toBe('sub123');
          expect(result1.identityProvider).toBe('AD');
          expect(result1.firstName).toBe('First');
          expect(result1.lastName).toBe('Last');
          expect(result1.roles).toBeDefined();
          expect(result1.roles.length).toBe(2);
          expect(result1.roles[0]).toBe('admin');
          expect(result1.roles[1]).toBe('superuser');
          expect(result1.entityType).toBe('user');

          let result2 = searchResults[1];
          expect(result2.groupName).toBe('Group 1');
          expect(result2.roles).toBeDefined();
          expect(result2.roles.length).toBe(1);
          expect(result2.roles[0]).toBe('viewer');
          expect(result2.entityType).toBe('group');
        });

        const req = httpTestingController.expectOne(`${FabricAuthMemberSearchService.baseMemberSearchApiUrl}?clientId=atlas`);
        expect(req.request.method).toBe("GET");      
        req.flush(mockAuthSearchResult, {status: 200, statusText: 'OK'});
        httpTestingController.verify();
      })
    )
  );
});
