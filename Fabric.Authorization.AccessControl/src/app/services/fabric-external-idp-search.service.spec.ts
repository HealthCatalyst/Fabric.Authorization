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
import { TestBed, inject, async } from '@angular/core/testing';
import { Observable } from 'rxjs/Observable';

import {
  FabricExternalIdpSearchService,
  AccessControlConfigService
} from '../services';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';
import { mockExternalIdpSearchResult } from './fabric-external-idp-search.service.mock';

describe('FabricExternalIdpSearchService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FabricExternalIdpSearchService,
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
      [FabricExternalIdpSearchService],
      (service: FabricExternalIdpSearchService) => {
        expect(service).toBeTruthy();
      }
    )
  );

  it(
    'searchExternalIdP should deserialize all properties',
    async(
      inject(
        [HttpClient, HttpTestingController, FabricExternalIdpSearchService],
        (
          httpClient: HttpClient,
          httpTestingController: HttpTestingController,
          service: FabricExternalIdpSearchService
        ) => {
          service.searchExternalIdP('sub', 'user').subscribe(searchResult => {
            expect(searchResult).toBeDefined();
            expect(searchResult.principals.length).toBe(2);

            const result1 = searchResult.principals[0];
            expect(result1.subjectId).toBe('sub123');
            expect(result1.firstName).toBe('First_1');
            expect(result1.lastName).toBe('Last_1');
            expect(result1.principalType).toBe('user');

            const result2 = searchResult.principals[1];
            expect(result2.subjectId).toBe('sub456');
            expect(result2.firstName).toBe('First_2');
            expect(result2.lastName).toBe('Last_2');
            expect(result2.principalType).toBe('user');
          });

          const req = httpTestingController.expectOne(
            `${
              FabricExternalIdpSearchService.idPServiceBaseUrl
            }?searchText=sub&type=user`
          );
          expect(req.request.method).toBe('GET');
          req.flush(mockExternalIdpSearchResult, {
            status: 200,
            statusText: 'OK'
          });
          httpTestingController.verify();
        }
      )
    )
  );
});
