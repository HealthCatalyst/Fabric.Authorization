import { TestBed, inject } from '@angular/core/testing';

import { FabricExternalIdpSearchInterceptorService } from './fabric-external-idp-search-interceptor.service';

describe('FabricExternalIdpSearchInterceptorService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FabricExternalIdpSearchInterceptorService]
    });
  });

  it('should be created', inject([FabricExternalIdpSearchInterceptorService], (service: FabricExternalIdpSearchInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
