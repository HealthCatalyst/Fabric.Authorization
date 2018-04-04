import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Observable';

import {
  FabricAuthMemberSearchService,
  AccessControlConfigService
} from '../services';
import { IAuthMemberSearchRequest, IRole, IAuthMemberSearchResult } from '../models';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';
import { mockAuthSearchResult } from './fabric-auth-member-search.service.mock';

describe('FabricAuthMemberSearchService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FabricAuthMemberSearchService,
        {
          provide: HTTP_INTERCEPTORS,
          useClass: FabricHttpErrorHandlerInterceptorService,
          multi: true
        },
        AccessControlConfigService
      ]
    });
  });

  it(
    'should be created',
    inject(
      [FabricAuthMemberSearchService],
      (service: FabricAuthMemberSearchService) => {
        expect(service).toBeTruthy();
      }
    )
  );

  it(
    'searchMembers should deserialize all properties',
    async(
      inject(
        [HttpClient, HttpTestingController, FabricAuthMemberSearchService],
        (
          httpClient: HttpClient,
          httpTestingController: HttpTestingController,
          service: FabricAuthMemberSearchService
        ) => {
          const authSearchRequest: IAuthMemberSearchRequest = {clientId: 'atlas', pageNumber: 1, grain: 'app', securableItem: 'Datamarts'};

          service.searchMembers(authSearchRequest).subscribe(searchResults => {
            expect(searchResults).toBeDefined();
            expect(searchResults.results.length).toBe(2);

            const result1 = searchResults.results[0];
            expect(result1.subjectId).toBe('sub123');
            expect(result1.identityProvider).toBe('AD');
            expect(result1.firstName).toBe('First');
            expect(result1.lastName).toBe('Last');
            expect(result1.roles).toBeDefined();
            expect(result1.roles.length).toBe(2);
            expect(result1.roles[0]).toEqual({ name: 'admin', grain: 'app', securableItem: 'foo'});
            expect(result1.roles[1]).toEqual(
              { name: 'superuser', grain: 'app', securableItem: 'foo'}
            );
            expect(result1.entityType).toBe('User');

            const result2 = searchResults.results[1];
            expect(result2.groupName).toBe('Group 2');
            expect(result2.roles).toBeDefined();
            expect(result2.roles.length).toBe(1);
            expect(result2.roles[0]).toEqual({name: 'viewer', grain: 'app', securableItem: 'foo'});
            expect(result2.entityType).toBe('CustomGroup');
          });

          const req = httpTestingController.expectOne(
            `${FabricAuthMemberSearchService.baseMemberSearchApiUrl}?clientId=atlas&grain=app&securableItem=Datamarts&pageNumber=1`);
          expect(req.request.method).toBe('GET');
          req.flush(mockAuthSearchResult, { status: 200, statusText: 'OK' });
          httpTestingController.verify();
        }
      )
    )
  );
});
