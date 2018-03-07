import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthUserService } from '../services';

describe('FabricAuthUserService', () => {

  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ HttpClientTestingModule ],
      providers: [FabricAuthUserService]
    });

    httpClient = TestBed.get(HttpClient);
    httpTestingController = TestBed.get(HttpTestingController);
  });

  it('should be created', inject([FabricAuthUserService], (service: FabricAuthUserService) => {
    expect(service).toBeTruthy();
  }));

  it('can get user roles', inject([FabricAuthUserService], (service: FabricAuthUserService) => {

  }));

  afterEach(() => {
    httpTestingController.verify();
  });

});
