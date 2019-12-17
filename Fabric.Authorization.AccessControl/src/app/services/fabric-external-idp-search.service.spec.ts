import { Subject } from 'rxjs';
import {
  HttpClient,
  HTTP_INTERCEPTORS
} from '@angular/common/http';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';
import { TestBed, inject, async } from '@angular/core/testing';

import { FabricHttpErrorHandlerInterceptorService } from './interceptors/fabric-http-error-handler-interceptor.service';
import { mockExternalIdpSearchResult } from './fabric-external-idp-search.service.mock';
import { FabricExternalIdpSearchService } from './fabric-external-idp-search.service';
import { MockAccessControlConfigService } from './access-control-config.service.mock';
import { AlertService } from './global/alert.service';
import { ToasterModule } from '@healthcatalyst/cashmere';
import { OverlayModule } from '@angular/cdk/overlay';

describe('FabricExternalIdpSearchService', () => {
  let searchTextSubject: Subject<string>;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, ToasterModule, OverlayModule],
      providers: [
        FabricExternalIdpSearchService,
        AlertService,
        {
          provide: HTTP_INTERCEPTORS,
          useClass: FabricHttpErrorHandlerInterceptorService,
          multi: true
        },
        {
          provide: 'IAccessControlConfigService',
          useClass: MockAccessControlConfigService
        }
      ]
    });
  });

  beforeEach(() => {
    searchTextSubject = new Subject<string>();
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
    'search should deserialize all properties',
    async(
      inject(
        [HttpClient, HttpTestingController, FabricExternalIdpSearchService],
        (
          httpClient: HttpClient,
          httpTestingController: HttpTestingController,
          service: FabricExternalIdpSearchService
        ) => {
          service.search(searchTextSubject.asObservable(), 'user').subscribe(searchResult => {
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

            const req = httpTestingController.expectOne(`${FabricExternalIdpSearchService.idPServiceBaseUrl}?searchText=sub&type=user`);

            expect(req.request.method).toBe('GET');
            req.flush(mockExternalIdpSearchResult, {
              status: 200,
              statusText: 'OK'
            });
            httpTestingController.verify();
          });
          searchTextSubject.next('sub');
        }
      )
    )
  );
});
