import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthMemberSearchService } from './fabric-auth-member-search.service';

describe('FabricAuthMemberSearchService', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [FabricAuthMemberSearchService]
    });

    httpClient = TestBed.get(HttpClient);
    httpTestingController = TestBed.get(HttpTestingController);
  });

  it('should be created', inject([FabricAuthMemberSearchService], (service: FabricAuthMemberSearchService) => {
    expect(service).toBeTruthy();
  }));
});
