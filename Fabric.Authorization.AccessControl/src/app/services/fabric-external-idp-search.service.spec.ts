import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed, inject } from '@angular/core/testing';

import { FabricExternalIdpSearchService } from './fabric-external-idp-search.service';

describe('FabricExternalIdpSearchService', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [FabricExternalIdpSearchService]
    });

    httpClient = TestBed.get(HttpClient);
    httpTestingController = TestBed.get(HttpTestingController);
  });

  it('should be created', inject([FabricExternalIdpSearchService], (service: FabricExternalIdpSearchService) => {
    expect(service).toBeTruthy();
  }));
});
