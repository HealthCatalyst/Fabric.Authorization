import { TestBed, inject } from '@angular/core/testing';

import { FabricAuthRequestInterceptorService } from './fabric-auth-request-interceptor.service';

describe('FabricAuthRequestInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricAuthRequestInterceptorService]
    });
  });

  it('should be created', inject([FabricAuthRequestInterceptorService], (service: FabricAuthRequestInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
